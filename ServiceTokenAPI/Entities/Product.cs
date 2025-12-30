using ServiceTokenAPI.Enums;

namespace ServiceTokenAPI.Entities
{
    public class Product
    {
        public long Id { get; set; }
        public long CompanyId { get; set; }
        public string Name { get; set; } = null!;
        public int TotalQuantity { get; set; }
        public decimal Price { get; set; }
        public int? Term { get; set; }
        public ScheduleType ScheduleType { get; set; } = null!;
    }
}