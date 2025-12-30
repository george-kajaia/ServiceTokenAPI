namespace ServiceTokenAPI.Entities
{
    public class CompanyUser
    {
        public int Id { get; set; }
        public long CompanyId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
