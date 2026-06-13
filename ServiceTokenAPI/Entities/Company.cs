using System.ComponentModel.DataAnnotations;


namespace ServiceTokenApi.Entities
{
    public class Company
    {
        public long Id { get; set; }

        [Timestamp]
        public uint RowVersion { get; set; }

        public byte Status { get; set; }

        public DateTime RegDate { get; set; }

        public string Name { get; set; } = string.Empty;

        public string TaxCode { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public byte LegalForm { get; set; }

        public int EconomicActivity { get; set; }

        public string Mail { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public ICollection<CompanyUser> Users { get; set; } = new List<CompanyUser>();
    }
}
