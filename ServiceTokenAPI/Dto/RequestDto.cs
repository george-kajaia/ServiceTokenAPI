using System.ComponentModel.DataAnnotations;
using ServiceTokenApi.Enums;

namespace ServiceTokenApi.Dto
{
    public class RequestDto
    {
        public long Id { get; set; }
        [Timestamp]
        public uint RowVersion { get; set; }
        public long CompanyId { get; set; }
        public string CompanyName { get; set; } = null!;
        public long ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public int ServiceTokenCount { get; set; }
        public DateTime RegDate { get; set; }
        public RequestStatus Status { get; set; }
        public DateTime? AuthorizeDate { get; set; }
        public DateTime? ApproveDate { get; set; }
    }

}
