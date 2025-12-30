namespace ServiceTokenAPI.Dto
{
    public class RequestDto
    {
        public long CompanyId { get; set; }
        public long ProdId { get; set; }
        public int TotalCount { get; set; }
        public decimal Price { get; set; }
        public decimal InterestRate { get; set; }
        public byte Term { get; set; }
        public byte RealizationPeriodNumber { get; set; }
    }
}
