using ServiceTokenApi.Enums;

namespace ServiceTokenApi.Services.Tbc
{
    /// <summary>
    /// Thin client over the TBC Bank E-Commerce (tpay) API. Handles access-token caching
    /// and the create / get / cancel payment endpoints. Persistence and business rules are
    /// the caller's responsibility (see PaymentController).
    /// </summary>
    public interface ITbcPaymentService
    {
        /// <summary>Creates a checkout payment and returns TBC's response (including the approval URL).</summary>
        Task<TbcPaymentResponse> CreatePaymentAsync(decimal amount, string merchantPaymentId, string? description = null, CancellationToken ct = default);

        /// <summary>Fetches the current status/details of a payment by its payId.</summary>
        Task<TbcPaymentResponse> GetPaymentAsync(string payId, CancellationToken ct = default);

        /// <summary>Cancels (fully or partially) a payment by its payId.</summary>
        Task<TbcPaymentResponse> CancelPaymentAsync(string payId, decimal? amount = null, CancellationToken ct = default);

        /// <summary>Maps a TBC status string to the platform's internal <see cref="PaymentStatus"/>.</summary>
        PaymentStatus MapStatus(string? tbcStatus);
    }
}
