using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTokenApi.DBContext;
using ServiceTokenApi.Dto;
using ServiceTokenApi.Entities;
using ServiceTokenApi.Enums;
using ServiceTokenApi.Services.Tbc;

namespace ServiceTokenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class PaymentController(
    ServiceTokenDbContext db,
    ITbcPaymentService tbc,
    ILogger<PaymentController> logger) : ControllerBase
{
    /// <summary>
    /// Initiates a TBC E-Commerce payment for a token the investor has already placed in
    /// their cart (Status = InCart). Records a Payment row, asks TBC to create the payment,
    /// and returns the checkout URL the client must redirect the buyer to.
    /// </summary>
    [HttpPost("InitiatePrimaryPayment")]
    public async Task<ActionResult<InitiatePaymentResultDto>> InitiatePrimaryPayment(string serviceTokenId, uint rowVersion, string investorPublicKey)
    {
        var serviceToken = await db.ServiceTokens.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == serviceTokenId
                                   && x.RowVersion == rowVersion
                                   && x.Status == ServiceTokenStatus.InCart);
        if (serviceToken is null) return NotFound("The record was changed by another user. Refresh the Data.");

        var price = await db.Products.AsNoTracking()
            .Where(x => x.Id == serviceToken.ProductId)
            .Select(x => x.Price)
            .SingleAsync();

        if (price <= 0) return BadRequest("Product price is not configured.");

        var merchantPaymentId = Guid.NewGuid().ToString("N");

        var payment = new Payment
        {
            MerchantPaymentId = merchantPaymentId,
            ServiceTokenId = serviceToken.Id,
            InvestorPublicKey = investorPublicKey,
            Amount = price,
            Currency = "GEL",
            Status = PaymentStatus.Created,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.Payments.Add(payment);
        await db.SaveChangesAsync();

        TbcPaymentResponse tbcResponse;
        try
        {
            tbcResponse = await tbc.CreatePaymentAsync(price, merchantPaymentId, $"Service token {serviceToken.Id}");
        }
        catch (TbcPaymentException ex)
        {
            logger.LogError(ex, "Failed to create TBC payment for token {TokenId}", serviceToken.Id);
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return StatusCode(StatusCodes.Status502BadGateway, $"Could not initiate payment with the bank: {ex.Message}");
        }

        payment.PayId = tbcResponse.PayId;
        payment.TbcStatus = tbcResponse.Status;
        payment.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        if (string.IsNullOrEmpty(tbcResponse.ApprovalUrl))
            return StatusCode(StatusCodes.Status502BadGateway, "Bank did not return a checkout URL.");

        return Ok(new InitiatePaymentResultDto
        {
            PayId = tbcResponse.PayId,
            ApprovalUrl = tbcResponse.ApprovalUrl
        });
    }

    /// <summary>
    /// Server-to-server callback invoked by TBC when a payment reaches a final status.
    /// Per TBC's contract we must return HTTP 200 on receipt and verify the real status via
    /// Get Payment rather than trusting the callback body. Finalization is idempotent.
    /// </summary>
    [HttpPost("Callback")]
    public async Task<IActionResult> Callback([FromBody] TbcCallbackPayload payload)
    {
        if (payload is null || string.IsNullOrWhiteSpace(payload.PaymentId))
            return Ok(); // acknowledge; nothing actionable

        var payment = await db.Payments.FirstOrDefaultAsync(x => x.PayId == payload.PaymentId);
        if (payment is null)
        {
            logger.LogWarning("TBC callback for unknown payId {PayId}", payload.PaymentId);
            return Ok();
        }

        // Already finalized — acknowledge idempotently.
        if (payment.Status is PaymentStatus.Succeeded or PaymentStatus.Failed
            or PaymentStatus.Cancelled or PaymentStatus.Expired)
            return Ok();

        TbcPaymentResponse details;
        try
        {
            details = await tbc.GetPaymentAsync(payload.PaymentId);
        }
        catch (TbcPaymentException ex)
        {
            // Leave the payment non-final so a later callback / poll can retry.
            logger.LogError(ex, "Failed to fetch TBC payment {PayId} during callback", payload.PaymentId);
            return Ok();
        }

        var status = tbc.MapStatus(details.Status);
        payment.TbcStatus = details.Status;
        payment.UpdatedAt = DateTime.UtcNow;

        if (status == PaymentStatus.Succeeded)
        {
            var finalized = await FinalizePrimaryPurchaseAsync(payment);
            payment.Status = finalized ? PaymentStatus.Succeeded : PaymentStatus.Failed;
        }
        else
        {
            payment.Status = status;
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // A concurrent finalization won the race; that's fine — acknowledge anyway.
            logger.LogWarning(ex, "Concurrent update finalizing payment {PayId}", payload.PaymentId);
        }

        return Ok();
    }

    /// <summary>Lets the client poll the outcome of a payment by payId.</summary>
    [HttpGet("GetStatus")]
    public async Task<IActionResult> GetStatus(string payId)
    {
        var payment = await db.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.PayId == payId);
        if (payment is null) return NotFound();

        return Ok(new
        {
            payment.PayId,
            payment.ServiceTokenId,
            Status = payment.Status.ToString(),
            payment.Amount,
            payment.Currency
        });
    }

    /// <summary>
    /// Mirrors ServiceTokenController.BuyPrimaryServiceToken: marks the token Sold, assigns
    /// dates/owner, logs the BuyPrimary operation and clears it from the cart. Returns false
    /// if the token is no longer purchasable (e.g. removed or sold concurrently).
    /// </summary>
    private async Task<bool> FinalizePrimaryPurchaseAsync(Payment payment)
    {
        var serviceToken = await db.ServiceTokens
            .FirstOrDefaultAsync(x => x.Id == payment.ServiceTokenId && x.Status == ServiceTokenStatus.InCart);
        if (serviceToken is null)
        {
            logger.LogWarning("Paid token {TokenId} no longer in cart at finalization", payment.ServiceTokenId);
            return false;
        }

        var term = (await db.Products
                .Where(x => x.Id == serviceToken.ProductId)
                .Select(x => x.Term)
                .SingleAsync())
            .GetValueOrDefault();

        var startDate = DateTime.UtcNow.Date;
        serviceToken.Status = ServiceTokenStatus.Sold;
        serviceToken.StartDate = startDate;
        serviceToken.EndDate = startDate.AddMonths(term);
        serviceToken.OwnerType = OwnerType.Investor;
        serviceToken.OwnerPublicKey = payment.InvestorPublicKey;

        db.Operations.Add(new Operation
        {
            ServiceTokenId = serviceToken.Id,
            OpType = OpType.BuyPrimary,
            OpDate = DateTime.UtcNow,
            OwnerPublicKey = serviceToken.OwnerPublicKey
        });

        var inCart = await db.ServiceTokenInCart.FirstOrDefaultAsync(x => x.ServiceTokenId == serviceToken.Id);
        if (inCart is not null) db.ServiceTokenInCart.Remove(inCart);

        return true;
    }
}
