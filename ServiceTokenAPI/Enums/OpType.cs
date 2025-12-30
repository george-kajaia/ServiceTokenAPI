namespace ServiceTokenAPI.Enums
{
    public enum OpType : byte
    {
        Issue = 0,
        BuyPrimary = 1,
        MarkForResell = 2,
        BuySecondary = 3,
        Expire = 255
    }
}
