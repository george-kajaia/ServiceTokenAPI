using ServiceTokenApi.Enums;

namespace ServiceTokenApi.Entities
{
    public class Request
    {
        public long Id { get; set; }
        public DateTime RowVersion { get; set; }
        public long CompanyId { get; set; }
        public long ProdId { get; set; }
        public DateTime RegDate { get; set; }
        public RequestStatus Status { get; set; }       
        public DateTime? AuthorizeDate { get; set; }
        public DateTime? ApproveDate { get; set; }
    }
}