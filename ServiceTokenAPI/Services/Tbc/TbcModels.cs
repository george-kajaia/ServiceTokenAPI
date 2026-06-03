using System.Text.Json.Serialization;

namespace ServiceTokenApi.Services.Tbc
{
    // ───── Access token ─────────────────────────────────────────────────────────

    internal sealed class TbcAccessTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
    }

    // ───── Create payment request ────────────────────────────────────────────────

    public sealed class TbcAmount
    {
        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "GEL";

        [JsonPropertyName("total")]
        public decimal Total { get; set; }
    }

    public sealed class TbcCreatePaymentRequest
    {
        [JsonPropertyName("amount")]
        public TbcAmount Amount { get; set; } = new();

        [JsonPropertyName("returnurl")]
        public string ReturnUrl { get; set; } = string.Empty;

        [JsonPropertyName("callbackUrl")]
        public string? CallbackUrl { get; set; }

        [JsonPropertyName("preAuth")]
        public bool PreAuth { get; set; }

        [JsonPropertyName("language")]
        public string Language { get; set; } = "EN";

        [JsonPropertyName("merchantPaymentId")]
        public string? MerchantPaymentId { get; set; }

        [JsonPropertyName("expirationMinutes")]
        public int? ExpirationMinutes { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    // ───── Payment response (create / details) ───────────────────────────────────

    public sealed class TbcLink
    {
        [JsonPropertyName("uri")]
        public string Uri { get; set; } = string.Empty;

        [JsonPropertyName("method")]
        public string Method { get; set; } = string.Empty;

        [JsonPropertyName("rel")]
        public string Rel { get; set; } = string.Empty;
    }

    public sealed class TbcPaymentResponse
    {
        [JsonPropertyName("payId")]
        public string PayId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("links")]
        public List<TbcLink> Links { get; set; } = [];

        [JsonPropertyName("transactionId")]
        public string? TransactionId { get; set; }

        [JsonPropertyName("recId")]
        public string? RecId { get; set; }

        [JsonPropertyName("preAuth")]
        public bool? PreAuth { get; set; }

        [JsonPropertyName("httpStatusCode")]
        public int? HttpStatusCode { get; set; }

        [JsonPropertyName("developerMessage")]
        public string? DeveloperMessage { get; set; }

        [JsonPropertyName("userMessage")]
        public string? UserMessage { get; set; }

        /// <summary>The checkout URL the buyer must be redirected to (links[rel=approval_url]).</summary>
        [JsonIgnore]
        public string? ApprovalUrl =>
            Links.FirstOrDefault(l => string.Equals(l.Rel, "approval_url", StringComparison.OrdinalIgnoreCase))?.Uri;
    }

    // ───── Callback payload ──────────────────────────────────────────────────────

    public sealed class TbcCallbackPayload
    {
        [JsonPropertyName("PaymentId")]
        public string PaymentId { get; set; } = string.Empty;
    }
}
