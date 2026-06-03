using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ServiceTokenApi.Enums;
using ServiceTokenApi.Options;

namespace ServiceTokenApi.Services.Tbc
{
    /// <summary>
    /// Raised when a call to the TBC API fails. Carries a safe, user-facing-ish message;
    /// full diagnostics are logged separately.
    /// </summary>
    public sealed class TbcPaymentException(string message) : Exception(message);

    public sealed class TbcPaymentService(HttpClient http, TbcTokenCache tokenCache, IOptions<TbcOptions> options, ILogger<TbcPaymentService> logger) : ITbcPaymentService
    {
        private readonly TbcOptions _options = options.Value;

        private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

        public async Task<TbcPaymentResponse> CreatePaymentAsync(decimal amount, string merchantPaymentId, string? description = null, CancellationToken ct = default)
        {
            var request = new TbcCreatePaymentRequest
            {
                Amount = new TbcAmount { Currency = _options.Currency, Total = amount },
                ReturnUrl = _options.ReturnUrl,
                CallbackUrl = string.IsNullOrWhiteSpace(_options.CallbackUrl) ? null : _options.CallbackUrl,
                PreAuth = false,
                Language = _options.Language,
                MerchantPaymentId = merchantPaymentId,
                ExpirationMinutes = _options.ExpirationMinutes,
                Description = description
            };

            using var msg = new HttpRequestMessage(HttpMethod.Post, "tpay/payments")
            {
                Content = JsonContent.Create(request, options: JsonOpts)
            };
            await AuthorizeAsync(msg, ct);

            using var resp = await http.SendAsync(msg, ct);
            return await ReadPaymentResponseAsync(resp, "create payment", ct);
        }

        public async Task<TbcPaymentResponse> GetPaymentAsync(string payId, CancellationToken ct = default)
        {
            using var msg = new HttpRequestMessage(HttpMethod.Get, $"tpay/payments/{payId}");
            await AuthorizeAsync(msg, ct);

            using var resp = await http.SendAsync(msg, ct);
            return await ReadPaymentResponseAsync(resp, "get payment", ct);
        }

        public async Task<TbcPaymentResponse> CancelPaymentAsync(string payId, decimal? amount = null, CancellationToken ct = default)
        {
            using var msg = new HttpRequestMessage(HttpMethod.Post, $"tpay/payments/{payId}/cancel")
            {
                Content = amount is null
                    ? JsonContent.Create(new { }, options: JsonOpts)
                    : JsonContent.Create(new { amount }, options: JsonOpts)
            };
            await AuthorizeAsync(msg, ct);

            using var resp = await http.SendAsync(msg, ct);
            return await ReadPaymentResponseAsync(resp, "cancel payment", ct);
        }

        // NOTE: TBC's exact status vocabulary is defined in their "Result Code" classification.
        // "Succeeded" is treated as the only success state; verify the remaining strings against
        // https://developers.tbcbank.ge/docs/result-code if you rely on the non-success branches.
        public PaymentStatus MapStatus(string? tbcStatus) => tbcStatus?.Trim().ToLowerInvariant() switch
        {
            "succeeded" => PaymentStatus.Succeeded,
            "failed" or "declined" => PaymentStatus.Failed,
            "expired" => PaymentStatus.Expired,
            "cancelled" or "canceled" or "returned" => PaymentStatus.Cancelled,
            "created" => PaymentStatus.Created,
            null or "" => PaymentStatus.None,
            _ => PaymentStatus.Processing
        };

        // ───── helpers ───────────────────────────────────────────────────────────

        private async Task AuthorizeAsync(HttpRequestMessage msg, CancellationToken ct)
        {
            var token = await GetAccessTokenAsync(ct);
            msg.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private async Task<string> GetAccessTokenAsync(CancellationToken ct)
        {
            if (tokenCache.IsValid) return tokenCache.AccessToken!;

            await tokenCache.Gate.WaitAsync(ct);
            try
            {
                if (tokenCache.IsValid) return tokenCache.AccessToken!;

                using var content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["client_id"] = _options.ClientId,
                    ["client_secret"] = _options.PurchaseSecretKey
                });

                using var resp = await http.PostAsync("tpay/access-token", content, ct);
                if (!resp.IsSuccessStatusCode)
                {
                    var body = await resp.Content.ReadAsStringAsync(ct);
                    logger.LogError("TBC access-token request failed: {Status} {Body}", (int)resp.StatusCode, body);
                    throw new TbcPaymentException($"Failed to obtain TBC access token (HTTP {(int)resp.StatusCode}): {Trim(body)}");
                }

                var token = await resp.Content.ReadFromJsonAsync<TbcAccessTokenResponse>(JsonOpts, ct)
                            ?? throw new TbcPaymentException("Empty access-token response from TBC.");

                tokenCache.AccessToken = token.AccessToken;
                // Refresh a minute early to avoid edge-of-expiry failures.
                tokenCache.ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, token.ExpiresIn - 60));
                return tokenCache.AccessToken!;
            }
            finally
            {
                tokenCache.Gate.Release();
            }
        }

        private async Task<TbcPaymentResponse> ReadPaymentResponseAsync(HttpResponseMessage resp, string op, CancellationToken ct)
        {
            var raw = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode)
            {
                logger.LogError("TBC {Op} failed: {Status} {Body}", op, (int)resp.StatusCode, raw);
                throw new TbcPaymentException($"TBC {op} failed (HTTP {(int)resp.StatusCode}): {Trim(raw)}");
            }

            return JsonSerializer.Deserialize<TbcPaymentResponse>(raw, JsonOpts)
                   ?? throw new TbcPaymentException($"Empty response from TBC {op}.");
        }

        // Keeps surfaced error detail short so it's safe to bubble up to the client during integration.
        private static string Trim(string? body) =>
            string.IsNullOrWhiteSpace(body) ? "(no detail)"
            : body.Length > 300 ? body[..300] : body;
    }
}
