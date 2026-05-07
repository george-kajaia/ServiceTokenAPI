namespace ServiceTokenApi.Enums
{
    public enum ServiceTokenStatus : byte
    {
        None = 0, // None, All
        Available = 1,  // for sell
        Sold = 2,       // Sold and have an owner
        InCart = 3,       // In Cart but not yet sold
        Finished = 255  // Not serviced
    }
}
