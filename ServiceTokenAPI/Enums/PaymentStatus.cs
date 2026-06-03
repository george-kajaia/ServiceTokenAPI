namespace ServiceTokenApi.Enums
{
    /// <summary>
    /// Internal lifecycle status of a TBC payment, decoupled from TBC's own status strings
    /// so the rest of the platform never has to know the bank's vocabulary.
    /// </summary>
    public enum PaymentStatus : byte
    {
        None = 0,
        Created = 1,     // initiated; buyer redirected to the TBC checkout page
        Processing = 2,  // TBC reports an in-progress / non-final state
        Succeeded = 3,   // funds captured; token ownership finalized
        Failed = 4,      // declined or errored
        Expired = 5,     // buyer never completed in time
        Cancelled = 6    // cancelled by merchant or buyer
    }
}
