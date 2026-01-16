using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceTokenApi.Entities;

namespace ServiceTokenApi.Configurations
{
    public class ServiceTokenConfiguration : IEntityTypeConfiguration<ServiceToken>
    {
        public void Configure(EntityTypeBuilder<ServiceToken> builder)
        {
            builder.ToTable("ServiceTokens");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => x.CompanyId).IsUnique(false);
            builder.HasIndex(x => x.RequestId).IsUnique(false);
            builder.HasIndex(x => x.ProdId).IsUnique(false);
            builder.Property(x => x.RowVersion).IsRowVersion();
        }
    }
}