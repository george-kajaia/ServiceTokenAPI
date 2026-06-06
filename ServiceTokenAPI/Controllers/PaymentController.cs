using System.Net.Mime;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ServiceTokenApi.DBContext;
using ServiceTokenApi.Dto;
using ServiceTokenApi.Entities;
using ServiceTokenApi.Enums;
using ServiceTokenApi.Options;
using ServiceTokenApi.Services.Flitt;

namespace ServiceTokenApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class PaymentController(
    ServiceTokenDbContext db,
    IFlittPaymentService flitt,
    IOptions<FlittOptions> flittOptions,
    ILogger<PaymentController> logger) : ControllerBase
{
    private readonly FlittOptions _options = flittOptions.Value;

    /// <summary>
    /// Initiates ONE Flitt payment for one or more in-cart tokens at once. Each token must still be
    /// InCart at the supplied row version. The buyer is charged a single summed amount and sees
    /// the embedded checkout once, but every token is recorded as its own <see cref="Payment"/>
    /// row sharing the same <c>order_id</c> (<see cref="Payment.MerchantPaymentId"/>). Returns the
    /// checkout token the client feeds to the embedded widget plus the order id used for polling.
    /// </summary>
    [HttpPost("InitiateEmbeddedPaymentBatch")]
    public async Task<ActionResult<InitiatePaymentResultDto>> InitiateEmbeddedPaymentBatch([FromBody] InitiatePaymentBatchRequestDto request)
    {
        if (request?.Tokens is null || request.Tokens.Count == 0)
            return BadRequest("No tokens supplied for payment.");

        if (string.IsNullOrWhiteSpace(request.InvestorPublicKey))
            return BadRequest("Investor public key is required.");

        var refs = request.Tokens
            .Where(t => !string.IsNullOrWhiteSpace(t.ServiceTokenId))
            .GroupBy(t => t.ServiceTokenId)
            .Select(g => g.First())
            .ToList();

        if (refs.Count == 0) return BadRequest("No valid tokens supplied for payment.");

        var tokenIds = refs.Select(x => x.ServiceTokenId).ToList();

        var serviceTokens = await db.ServiceTokens
            .Where(x => tokenIds.Contains(x.Id))
            .ToListAsync();

        var lineItems = new List<(string TokenId, decimal Price)>();
        foreach (var tokenRef in refs)
        {
            var token = serviceTokens.Where(x => x.Id == tokenRef.ServiceTokenId && x.RowVersion == tokenRef.RowVersion).FirstOrDefault();
            if (token is null)
                return NotFound($"Token {tokenRef.ServiceTokenId} was changed by another user. Refresh the data.");

            if (token.Price <= 0)
                return BadRequest($"Product price is not configured for token {tokenRef.ServiceTokenId}.");

            lineItems.Add((token.Id, token.Price));
        }

        var total = lineItems.Sum(x => x.Price);
        if (total <= 0) return BadRequest("Order total must be greater than zero.");

        // One order id, shared by every per-token Payment row and sent once to Flitt.
        var orderId = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;

        var payments = lineItems.Select(li => new Payment
        {
            MerchantPaymentId = orderId,
            ServiceTokenId = li.TokenId,
            InvestorPublicKey = request.InvestorPublicKey,
            Amount = li.Price,
            Currency = "GEL",
            Status = PaymentStatus.Created,
            CreatedAt = now,
            UpdatedAt = now
        }).ToList();

        db.Payments.AddRange(payments);
        await db.SaveChangesAsync();

        FlittTokenResult tokenResult;
        try
        {
            var description = payments.Count == 1
                ? $"Service token {payments[0].ServiceTokenId}"
                : $"{payments.Count} service tokens";

            // The payment provider is invoked exactly once, for the summed amount.
            tokenResult = await flitt.CreateOrderAsync(total, orderId, description);
        }
        catch (FlittPaymentException ex)
        {
            logger.LogError(ex, "Failed to create Flitt order {OrderId} for {Count} token(s)", orderId, payments.Count);
            var failedAt = DateTime.UtcNow;
            foreach (var p in payments)
            {
                p.Status = PaymentStatus.Failed;
                p.UpdatedAt = failedAt;
            }
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
    /// with HTTP 200, verify the signature against the purchase secret key, and finalize every
    /// token in the order on an approved status. Finalization is idempotent.
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

        var payments = await db.Payments.Where(x => x.MerchantPaymentId == orderId).ToListAsync();
        if (payments.Count == 0)
        {
            logger.LogWarning("Flitt callback for unknown order {OrderId}", orderId);
            return Ok();
        }

        await ApplyOutcomeAsync(payments,
            orderStatus: parameters.GetValueOrDefault("order_status"),
            paymentId: parameters.GetValueOrDefault("payment_id"));

        return Ok();
    }

    /// <summary>
    /// Browser-facing return URL Flitt redirects to (typically via POST) after a hard-redirect
    /// step such as 3-D Secure. nginx serves the SPA as static files and rejects a POST with
    /// 405, so this endpoint absorbs the request and 303-redirects the browser — as a GET — to
    /// the SPA result page (<see cref="FlittOptions.ClientReturnUrl"/>), carrying the order id so
    /// the page can poll its status. It performs no finalization; that is the callback's / poll's
    /// job, so it deliberately accepts both POST and GET and never trusts the posted body.
    /// </summary>
    [HttpPost("Return")]
    [HttpGet("Return")]
    public async Task<IActionResult> Return()
    {
        string? orderId = null;

        // Flitt posts the result as form-encoded data on the hard redirect.
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            orderId = FirstNonEmpty(form["order_id"], form["orderId"]);
        }
        // Fall back to the query string (covers GET returns and manual navigation).
        orderId ??= FirstNonEmpty(Request.Query["order_id"], Request.Query["orderId"]);

        var clientUrl = string.IsNullOrWhiteSpace(_options.ClientReturnUrl)
            ? "/"
            : _options.ClientReturnUrl;

        var target = string.IsNullOrWhiteSpace(orderId)
            ? clientUrl
            : QueryHelpers.AddQueryString(clientUrl, "orderId", orderId);

        // 303 See Other forces the browser to issue a GET to the SPA (so nginx serves it fine),
        // regardless of whether Flitt arrived here via POST or GET.
        Response.Headers.Location = target;
        return StatusCode(StatusCodes.Status303SeeOther);
    }

    /// <summary>
    /// Lets the client poll the outcome of a payment by our order_id. Because the Flitt server
    /// callback cannot reach a non-public host (e.g. localhost in development), this endpoint
    /// also reconciles status directly with Flitt's order-status API while the order is not yet
    /// terminal. Returns the overall headline status, a <c>final</c> flag (true only once every
    /// token is terminal) and the per-token breakdown.
    /// </summary>
    [HttpGet("GetStatus")]
    public async Task<IActionResult> GetStatus(string orderId)
    {
        var payments = await db.Payments
            .Where(x => x.MerchantPaymentId == orderId)
            .ToListAsync();
        if (payments.Count == 0) return NotFound();

        if (payments.Any(p => !IsFinal(p.Status)))
        {
            try
            {
                var order = await flitt.GetOrderStatusAsync(orderId);
                if (order.ErrorCode is null && (order.SignatureValid || order.OrderStatus is not null))
                {
                    await ApplyOutcomeAsync(payments, order.OrderStatus, order.PaymentId);
                }
            }
            catch (FlittPaymentException ex)
            {
                // Don't fail the poll on a transient lookup error — just return what we have.
                logger.LogDebug(ex, "Order-status reconcile failed for {OrderId}", orderId);
            }
        }

        var final = payments.All(p => IsFinal(p.Status));
        return Ok(new PaymentBatchStatusDto
        {
            OrderId = orderId,
            Status = OverallStatus(payments, final).ToString(),
            Final = final,
            Amount = payments.Sum(p => p.Amount),
            Currency = payments[0].Currency,
            Items = payments
                .OrderBy(p => p.Id)
                .Select(p => new PaymentItemStatusDto
                {
                    ServiceTokenId = p.ServiceTokenId,
                    Status = p.Status.ToString()
                })
                .ToList()
        });
    }

    // ───── helpers ────────────────────────────────────────────────────────────

    private static bool IsFinal(PaymentStatus s) =>
        s is PaymentStatus.Succeeded or PaymentStatus.Failed
          or PaymentStatus.Cancelled or PaymentStatus.Expired;

    private static string? FirstNonEmpty(params Microsoft.Extensions.Primitives.StringValues[] values)
    {
        foreach (var v in values)
        {
            var s = v.ToString();
            if (!string.IsNullOrWhiteSpace(s)) return s;
        }
        return null;
    }

    /// <summary>
    /// Collapses the per-token statuses of an order into a single headline status. The order is
    /// only ever reported as non-final (Processing) while any token is still pending; once all
    /// tokens are terminal it surfaces success if at least one token was captured, otherwise the
    /// shared terminal reason (cancelled/expired) or a generic Failed.
    /// </summary>
    private static PaymentStatus OverallStatus(List<Payment> payments, bool final)
    {
        if (!final) return PaymentStatus.Processing;
        if (payments.Any(p => p.Status == PaymentStatus.Succeeded)) return PaymentStatus.Succeeded;
        if (payments.All(p => p.Status == PaymentStatus.Cancelled)) return PaymentStatus.Cancelled;
        if (payments.All(p => p.Status == PaymentStatus.Expired)) return PaymentStatus.Expired;
        return PaymentStatus.Failed;
    }

    /// <summary>
    /// Applies a Flitt order_status to every (non-terminal) token in the order, finalizing each
    /// purchase on success. Idempotent: tokens already in a terminal state are left untouched, so
    /// a duplicate callback / poll is harmless. All changes are persisted in a single save.
    /// </summary>
    private async Task ApplyOutcomeAsync(List<Payment> payments, string? orderStatus, string? paymentId)
    {
        var pending = payments.Where(p => !IsFinal(p.Status)).ToList();
        if (pending.Count == 0) return;

        var status = flitt.MapStatus(orderStatus);
        var now = DateTime.UtcNow;

        foreach (var payment in pending)
        {
            payment.ProviderStatus = orderStatus;
            if (!string.IsNullOrEmpty(paymentId)) payment.PayId = paymentId;
            payment.UpdatedAt = now;

            if (status == PaymentStatus.Succeeded)
            {
                // Each token is finalized independently: one token that is no longer purchasable
                // fails on its own without blocking the others in the same order.
                var finalized = await FinalizePrimaryPurchaseAsync(payment);
                payment.Status = finalized ? PaymentStatus.Succeeded : PaymentStatus.Failed;
            }
            else
            {
                payment.Status = status;
            }
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // A concurrent finalization won the race; that's fine.
            logger.LogWarning(ex, "Concurrent update finalizing order {OrderId}", payments[0].MerchantPaymentId);
        }
    }

    /// <summary>
    /// Mirrors ServiceTokenController.BuyPrimaryServiceToken: marks the token Sold, assigns
    /// dates/owner, logs the BuyPrimary operation and clears it from the cart. Returns false
    /// if the token is no longer purchasable (e.g. removed or sold concurrently). Does NOT save —
    /// the caller persists all tokens in the order together.
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
