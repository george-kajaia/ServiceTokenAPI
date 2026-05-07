using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceTokenApi.Entities;

namespace ServiceTokenApi.Configurations
{
    public class ServiceTokenInCartConfiguration : IEntityTypeConfiguration<ServiceTokenInCart>
    {
        public void Configure(EntityTypeBuilder<ServiceTokenInCart> builder)
        {
            builder.ToTable("ServiceTokensInCart");
            builder.HasKey(x => x.ServiceTokenId);
            builder.HasIndex(x => x.OwnerPublicKey).IsUnique(false);
        }
    }
}