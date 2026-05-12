using System.ComponentModel.DataAnnotations;
using ServiceTokenApi.Entities;
using ServiceTokenApi.Enums;

namespace ServiceTokenApi.Dto
{
    public class ServiceTokenDto
    {
        public string Id { get; set; } = null!;
        [Timestamp]
        public uint RowVersion { get; set; }
        public long CompanyId { get; set; }
        public string CompanyName { get; set; } = null!;
        public long RequestId { get; set; }
        public long ProductId { get; set; }
        public string ProductName { get; set; } = null!;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ServiceTokenStatus Status { get; set; }
        public int RemainingCount { get; set; }
        public int ServiceCount { get; set; }
        public ScheduleType ScheduleType { get; set; } = null!;
        public OwnerType OwnerType { get; set; }
        public string OwnerPublicKey { get; set; } = string.Empty;
        public byte[] Pictogram { get; set; } = [];
    }
}
