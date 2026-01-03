using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceTokenApi.Entities;

namespace ServiceTokenApi.Configurations
{
    public class OperationConfiguration : IEntityTypeConfiguration<Operation>
    {
        public void Configure(EntityTypeBuilder<Operation> builder)
        {
            builder.ToTable("Operations");
            builder.HasKey(item => item.Id);
            builder.HasIndex(u => u.ServiceTokenId).IsUnique(false);

            builder.HasOne<ServiceToken>()
                   .WithMany()
                   .HasForeignKey(x => x.ServiceTokenId)
                   .HasPrincipalKey(x => x.Id)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}