using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ServiceTokenApi.Services.Flitt
{
    // ───── Create order (checkout token) ────────────────────────────────────────

    /// <summary>Inner "request" object sent to POST /api/checkout/token.</summary>
    public sealed class FlittCreateOrderRequest
    {
        [JsonPropertyName("order_id")]
        public string OrderId { get; set; } = string.Empty;

        [JsonPropertyName("merchant_id")]
        public int MerchantId { get; set; }

        /// <summary>Amount in minor units (e.g. tetri / cents): 10.00 GEL → 1000.</summary>
        [JsonPropertyName("amount")]
        public long Amount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "GEL";

        [JsonPropertyName("order_desc")]
        public string OrderDesc { get; set; } = string.Empty;

        [JsonPropertyName("server_callback_url")]
        public string? ServerCallbackUrl { get; set; }

        [JsonPropertyName("response_url")]
        public string? ResponseUrl { get; set; }

        [JsonPropertyName("lifetime")]
        public int? Lifetime { get; set; }

        [JsonPropertyName("signature")]
        public string Signature { get; set; } = string.Empty;
    }

    /// <summary>Envelope wrapping the request: { "request": { ... } }.</summary>
    public sealed class FlittRequestEnvelope<T>
    {
        [JsonPropertyName("request")]
        public T Request { get; set; } = default!;
    }

    /// <summary>Parsed result of a create-order / token call.</summary>
    public sealed class FlittTokenResult
    {
        public string? Token { get; init; }
        public string ResponseStatus { get; init; } = string.Empty;
        public int? ErrorCode { get; init; }
        public string? ErrorMessage { get; init; }

        public bool IsSuccess => string.Equals(ResponseStatus, "success", StringComparison.OrdinalIgnoreCase)
                                 && !string.IsNullOrEmpty(Token);
    }

    // ───── Order status / callback ──────────────────────────────────────────────

    /// <summary>
    /// A flat bag of the parameters Flitt returns in a callback or order-status response.
    /// Kept as raw string values so the signature can be re-computed over the exact set
    /// of returned fields (the signature spans <i>all</i> non-empty parameters).
    /// </summary>
    public sealed class FlittOrderResult
    {
        public IReadOnlyDictionary<string, string> Parameters { get; init; } =
            new Dictionary<string, string>();

        public string? OrderId => Get("order_id");
        public string? OrderStatus => Get("order_status");
        public string? ResponseStatus => Get("response_status");
        public string? PaymentId => Get("payment_id");
        public string? Amount => Get("amount");
        public string? Currency => Get("currency");
        public string? Signature => Get("signature");
        public int? ErrorCode =>
            Parameters.TryGetValue("error_code", out var v) && int.TryParse(v, out var n) ? n : null;
        public string? ErrorMessage => Get("error_message");

        public bool SignatureValid { get; init; }

        private string? Get(string key) => Parameters.TryGetValue(key, out var v) ? v : null;
    }

    // ───── Signature helper ──────────────────────────────────────────────────────

    /// <summary>
    /// Builds and verifies Flitt signatures. The signature is SHA-1 over the merchant
    /// secret key followed by all non-empty parameters, sorted by key in ascending
    /// (ordinal) order and joined with '|'. The <c>signature</c> and
    /// <c>response_signature_string</c> parameters never take part in the calculation.
    /// </summary>
    public static class FlittSignature
    {
        public static string Build(string secretKey, IEnumerable<KeyValuePair<string, string?>> parameters)
        {
            var ordered = parameters
                .Where(kv => kv.Key is not ("signature" or "response_signature_string"))
                .Where(kv => !string.IsNullOrEmpty(kv.Value))
                .OrderBy(kv => kv.Key, StringComparer.Ordinal)
                .Select(kv => kv.Value!);

            var raw = string.Join("|", new[] { secretKey }.Concat(ordered));
            var hash = SHA1.HashData(Encoding.UTF8.GetBytes(raw));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        public static bool Verify(string secretKey, IReadOnlyDictionary<string, string> parameters)
        {
            if (!parameters.TryGetValue("signature", out var provided) || string.IsNullOrEmpty(provided))
                return false;

            var expected = Build(
                secretKey,
                parameters.Select(kv => new KeyValuePair<string, string?>(kv.Key, kv.Value)));

            return string.Equals(expected, provided, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Flattens a JSON object into string values suitable for signature computation:
        /// strings keep their value, numbers keep their raw text, booleans become
        /// "true"/"false", and null/empty entries are dropped.
        /// </summary>
        public static Dictionary<string, string> Flatten(IDictionary<string, JsonElement> json)
        {
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var (key, value) in json)
            {
                var s = value.ValueKind switch
                {
                    JsonValueKind.String => value.GetString(),
                    JsonValueKind.Number => value.GetRawText(),
                    JsonValueKind.True => "true",
                    JsonValueKind.False => "false",
                    JsonValueKind.Null => null,
                    JsonValueKind.Undefined => null,
                    _ => value.GetRawText()
                };
                if (!string.IsNullOrEmpty(s)) result[key] = s;
            }
            return result;
        }
    }
}
