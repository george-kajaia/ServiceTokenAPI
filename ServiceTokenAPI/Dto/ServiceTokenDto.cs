using ServiceTokenApi.Entities;

namespace ServiceTokenApi.Dto
{
    public class ServiceTokenDto : ServiceToken
    {
        public string CompanyName { get; set; } = null!;
    }
}
