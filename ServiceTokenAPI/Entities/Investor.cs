namespace ServiceTokenAPI.Entities
{
    public class Investor
    {
        public long Id { get; set; }
        public string PublicKey { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
