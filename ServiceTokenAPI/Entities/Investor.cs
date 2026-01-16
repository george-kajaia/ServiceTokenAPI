namespace ServiceTokenApi.Entities
{
    public class Investor
    {
        public long Id { get; set; }
        public DateTime RowVersion { get; set; }
        public string PublicKey { get; set; } = string.Empty;
        public byte Status { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
