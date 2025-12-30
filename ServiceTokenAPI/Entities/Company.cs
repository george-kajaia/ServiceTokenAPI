namespace ServiceTokenAPI.Entities
{
    public class Company
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public byte Status { get; set; }
        public DateTime RegDate { get; set; }
        public string TaxCode { get; set; } = string.Empty;
        public string? PublicKey { get; set; }
    }
}
