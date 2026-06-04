using ServiceTokenApi.Enums;

namespace ServiceTokenApi.Services.Flitt
{
    /// <summary>
    /// Thin client over the Flitt payment API. Creates checkout-order tokens for the
    /// embedded checkout, fetches order status (used to reconcile when the server callback
    /// can't reach us, e.g. in local development) and verifies callback signatures.
    /// Persistence and business rules are the caller's responsibility (see PaymentController).
    /// </summary>
    public interface IFlittPaymentService
    {
        /// <summary>
        /// Creates an order on Flitt and returns the checkout token the frontend feeds to the
        /// embedded checkout widget. <paramref name="amount"/> is in major units (e.g. GEL);
        /// it is converted to minor units for Flitt.
        /// </summary>
        Task<FlittTokenResult> CreateOrderAsync(decimal amount, string orderId, string description, CancellationToken ct = default);

        /// <summary>Fetches the current status of an order by our order_id, verifying the response signature.</summary>
        Task<FlittOrderResult> GetOrderStatusAsync(string orderId, CancellationToken ct = default);

        /// <summary>Verifies the signature of a callback / status parameter set against the purchase secret key.</summary>
        bool VerifySignature(IReadOnlyDictionary<string, string> parameters);

        /// <summary>Maps a Flitt order_status string to the platform's internal <see cref="PaymentStatus"/>.</summary>
        PaymentStatus MapStatus(string? orderStatus);
    }
}
