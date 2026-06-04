using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServiceTokenApi.DBContext;
using ServiceTokenApi.Dto;
using ServiceTokenApi.Entities;
using ServiceTokenApi.Enums;
using ServiceTokenApi.Services.Flitt;

namespace ServiceTokenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class PaymentController(
    ServiceTokenDbContext db,
    IFlittPaymentService flitt,
    ILogger<PaymentController> logger) : ControllerBase
{
    /// <summary>
    /// Initiates a Flitt payment for a token the investor has already placed in their cart
    /// (Status = InCart). Records a Payment row, asks Flitt to create an order, and returns the
    /// checkout token the client feeds to the embedded Flitt checkout widget (no redirect).
    /// </summary>
    [HttpPost("InitiateEmbeddedPayment")]
    public async Task<ActionResult<InitiatePaymentResultDto>> InitiateEmbeddedPayment(string serviceTokenId, uint rowVersion, string investorPublicKey)
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

        // order_id must be unique per order; Flitt limits it to a reasonable length.
        var orderId = Guid.NewGuid().ToString("N");

        var payment = new Payment
        {
            MerchantPaymentId = orderId,
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

        FlittTokenResult tokenResult;
        try
        {
            tokenResult = await flitt.CreateOrderAsync(price, orderId, $"Service token {serviceToken.Id}");
        }
        catch (FlittPaymentException ex)
        {
            logger.LogError(ex, "Failed to create Flitt order for token {TokenId}", serviceToken.Id);
            payment.Status = PaymentStatus.Failed;
            payment.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return StatusCode(StatusCodes.Status502BadGateway, $"Could not initiate payment: {ex.Message}");
        }

        return Ok(new InitiatePaymentResultDto
        {
            OrderId = orderId,
            Token = tokenResult.Token!
        });
    }

    /// <summary>
    /// Server-to-server callback Flitt POSTs to when an order changes status. We acknowledge
    /// with HTTP 200, verify the signature against the purchase secret key, and finalize the
    /// purchase on an approved status. Finalization is idempotent.
    /// </summary>
    [HttpPost("Callback")]
    public async Task<IActionResult> Callback([FromBody] Dictionary<string, JsonElement>? body)
    {
        if (body is null || body.Count == 0) return Ok(); // acknowledge; nothing actionable

        var parameters = FlittSignature.Flatten(body);

        if (!flitt.VerifySignature(parameters))
        {
            logger.LogWarning("Flitt callback with invalid signature for order {OrderId}",
                parameters.GetValueOrDefault("order_id"));
            return Ok(); // never leak detail; just acknowledge
        }

        var orderId = parameters.GetValueOrDefault("order_id");
        if (string.IsNullOrWhiteSpace(orderId)) return Ok();

        var payment = await db.Payments.FirstOrDefaultAsync(x => x.MerchantPaymentId == orderId);
        if (payment is null)
        {
            logger.LogWarning("Flitt callback for unknown order {OrderId}", orderId);
            return Ok();
        }

        await ApplyOutcomeAsync(payment,
            orderStatus: parameters.GetValueOrDefault("order_status"),
            paymentId: parameters.GetValueOrDefault("payment_id"));

        return Ok();
    }

    /// <summary>
    /// Lets the client poll the outcome of a payment by our order_id. Because the Flitt server
    /// callback cannot reach a non-public host (e.g. localhost in development), this endpoint
    /// also reconciles status directly with Flitt's order-status API while the payment is not
    /// yet in a terminal state.
    /// </summary>
    [HttpGet("GetStatus")]
    public async Task<IActionResult> GetStatus(string orderId)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(x => x.MerchantPaymentId == orderId);
        if (payment is null) return NotFound();

        if (!IsFinal(payment.Status))
        {
            try
            {
                var order = await flitt.GetOrderStatusAsync(orderId);
                if (order.ErrorCode is null && (order.SignatureValid || order.OrderStatus is not null))
                {
                    await ApplyOutcomeAsync(payment, order.OrderStatus, order.PaymentId);
                }
            }
            catch (FlittPaymentException ex)
            {
                // Don't fail the poll on a transient lookup error — just return what we have.
                logger.LogDebug(ex, "Order-status reconcile failed for {OrderId}", orderId);
            }
        }

        return Ok(new
        {
            OrderId = payment.MerchantPaymentId,
            payment.ServiceTokenId,
            Status = payment.Status.ToString(),
            payment.Amount,
            payment.Currency
        });
    }

    // ───── helpers ────────────────────────────────────────────────────────────

    private static bool IsFinal(PaymentStatus s) =>
        s is PaymentStatus.Succeeded or PaymentStatus.Failed
          or PaymentStatus.Cancelled or PaymentStatus.Expired;

    /// <summary>
    /// Applies a Flitt order_status to the payment, finalizing the purchase on success.
    /// Idempotent: once a payment is in a terminal state it is left untouched.
    /// </summary>
    private async Task ApplyOutcomeAsync(Payment payment, string? orderStatus, string? paymentId)
    {
        if (IsFinal(payment.Status)) return;

        var status = flitt.MapStatus(orderStatus);
        payment.ProviderStatus = orderStatus;
        if (!string.IsNullOrEmpty(paymentId)) payment.PayId = paymentId;
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
            // A concurrent finalization won the race; that's fine.
            logger.LogWarning(ex, "Concurrent update finalizing order {OrderId}", payment.MerchantPaymentId);
        }
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
