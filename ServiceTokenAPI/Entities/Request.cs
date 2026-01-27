using System.ComponentModel.DataAnnotations;
using ServiceTokenApi.Enums;

namespace ServiceTokenApi.Entities
{
    public class Request
    {
        public long Id { get; set; }
        [Timestamp]
        public uint RowVersion { get; set; }
        public long CompanyId { get; set; }
        public long ProductId { get; set; }
        public  int ServiceTokenCount { get; set; }
        public DateTime RegDate { get; set; }
        public RequestStatus Status { get; set; }       
        public DateTime? AuthorizeDate { get; set; }
        public DateTime? ApproveDate { get; set; }
    }
}