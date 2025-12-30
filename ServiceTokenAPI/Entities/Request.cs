using ServiceTokenAPI.Enums;

namespace ServiceTokenAPI.Entities
{
    public class Request
    {
        public long Id { get; set; }
        public DateTime RowVersion { get; set; }
        public long CompanyId { get; set; }
        public long ProdId { get; set; }
        public DateTime RegDate { get; set; }
        public RequestStatus Status { get; set; }
        public int TotalCount { get; set; }
        public decimal Price { get; set; }        
        public DateTime? ApproveDate { get; set; }        
    }
}