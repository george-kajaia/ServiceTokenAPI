using ServiceTokenApi.Enums;

namespace ServiceTokenApi.Entities
{
    public class ServiceToken
    {
        public string Id { get; set; } = null!;
        public DateTime RowVersion { get; set; }
        public long CompanyId { get; set; }
        public long RequestId { get; set; }
        public long ProdId { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public ServiceTokenStatus Status { get; set; }
        public int Count { get; set; }
        public int ServiceCount { get; set; }
        public ScheduleType ScheduleType { get; set; } = null!;
        public OwnerType OwnerType { get; set; }
        public string OwnerPublicKey { get; set; } = string.Empty;
    }
}
