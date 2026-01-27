namespace ServiceTokenApi.Dto
{
    public class NewRequestDto
    {
        public long CompanyId { get; set; }
        public long ProductId { get; set; }
        public int ServiceTokenCount { get; set; }
    }

}
