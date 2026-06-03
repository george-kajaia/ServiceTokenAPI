namespace ServiceTokenApi.Options
{
    /// <summary>
    /// Strongly-typed configuration for the TBC Bank E-Commerce (tpay) integration.
    /// Bound from the "Tbc" section of configuration. Secret values should be supplied
    /// through user-secrets / environment variables rather than committed appsettings.
    /// </summary>
    public class TbcOptions
    {
        public const string SectionName = "Tbc";

        /// <summary>Base URL including the API version, e.g. https://api.tbcbank.ge/v1/ (trailing slash required).</summary>
        public string BaseUrl { get; set; } = "https://api.tbcbank.ge/v1/";

        /// <summary>Developer-app API key, sent in the "apikey" header on every request.</summary>
        public string ApiKey { get; set; } = string.Empty;

        /// <summary>Merchant client_id (TBC refers to this as merchant_id).</summary>
        public string ClientId { get; set; } = string.Empty;

        /// <summary>client_secret used for the purchase (E-Commerce) flow.</summary>
        public string PurchaseSecretKey { get; set; } = string.Empty;

        /// <summary>client_secret used for the payout flow (reserved for future payout support).</summary>
        public string PayoutSecretKey { get; set; } = string.Empty;

        /// <summary>URL TBC redirects the buyer to after they finish on the checkout page.</summary>
        public string ReturnUrl { get; set; } = string.Empty;

        /// <summary>
        /// Server-to-server callback TBC POSTs to when a payment reaches a final status.
        /// Must also be registered on the TBC merchant dashboard.
        /// </summary>
        public string CallbackUrl { get; set; } = string.Empty;

        /// <summary>ISO currency code for payments (GEL, USD, EUR).</summary>
        public string Currency { get; set; } = "GEL";

        /// <summary>Checkout page language (KA or EN).</summary>
        public string Language { get; set; } = "EN";

        /// <summary>Payment initiation expiry in minutes. TBC recommends a maximum of 12.</summary>
        public int ExpirationMinutes { get; set; } = 12;
    }
}
