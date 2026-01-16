namespace ServiceTokenApi.Enums
{
    public enum ServiceTokenStatus : byte
    {
        Available = 0,  // for sell
        Sold = 1,       // Sold and have an owner
        Finished = 255  // Not serviced
    }
}
