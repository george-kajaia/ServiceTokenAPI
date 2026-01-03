using ServiceTokenApi.Enums;

namespace ServiceTokenApi.Entities
{
    public class Operation
    {
        public long Id { get; set; }
        public string ServiceTokenId { get; set; } = null!;
        public OpType OpType { get; set; }
        public DateTime OpDate { get; set; }
        public string OwnerPublicKey { get; set; } = null!;
    }
}
