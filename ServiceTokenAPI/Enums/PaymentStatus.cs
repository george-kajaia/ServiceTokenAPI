namespace ServiceTokenApi.Enums
{
    /// <summary>
    /// Internal lifecycle status of a payment, decoupled from the payment provider's own status
    /// strings so the rest of the platform never has to know the provider's vocabulary.
    /// </summary>
    public enum PaymentStatus : byte
    {
        None = 0,
        Created = 1,     // order created; checkout token issued to the client
        Processing = 2,  // provider reports an in-progress / non-final state
        Succeeded = 3,   // funds captured; token ownership finalized
        Failed = 4,      // declined or errored
        Expired = 5,     // buyer never completed in time
        Cancelled = 6    // cancelled / reversed
    }
}
