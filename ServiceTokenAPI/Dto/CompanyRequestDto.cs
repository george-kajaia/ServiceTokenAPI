namespace ServiceTokenApi.Dto
{
    public class CompanyRequestDto
    {
        public string Name { get; set; } = string.Empty;

        public string TaxCode { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public byte LegalForm { get; set; }

        public int EconomicActivity { get; set; }

        public string Mail { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;
        
        public string Password { get; set; } = string.Empty;
    }
}
