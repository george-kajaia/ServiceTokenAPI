namespace ServiceTokenApi.Enums
{
    public enum OpType : byte
    {
        Issue = 0,
        BuyPrimary = 1,
        MarkForResell = 2,
        BuySecondary = 3,
        CancelReselling = 4,
        GetService = 5,
        Expire = 255
    }
}
