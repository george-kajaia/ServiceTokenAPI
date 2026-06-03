namespace ServiceTokenApi.Services.Tbc
{
    /// <summary>
    /// Thread-safe in-memory cache for the TBC access token. Registered as a singleton so
    /// the (daily) token is shared across requests instead of being fetched on every call.
    /// </summary>
    public sealed class TbcTokenCache
    {
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public string? AccessToken { get; set; }
        public DateTimeOffset ExpiresAtUtc { get; set; }

        public bool IsValid => !string.IsNullOrEmpty(AccessToken) && DateTimeOffset.UtcNow < ExpiresAtUtc;
    }
}
