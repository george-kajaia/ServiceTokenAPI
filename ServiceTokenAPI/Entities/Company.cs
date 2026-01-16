using System.ComponentModel.DataAnnotations;

namespace ServiceTokenApi.Entities
{
    public class Company
    {
        public long Id { get; set; }
        
        [Timestamp] 
        public uint RowVersion { get; set; }
        
        public string Name { get; set; } = string.Empty;
        
        public byte Status { get; set; }
        
        public DateTime RegDate { get; set; }
        
        public string TaxCode { get; set; } = string.Empty;
        
        public CompanyUser? User { get; set; }
    }
}
