using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceTokenApi.Entities;

namespace ServiceTokenApi.Configurations
{
    public class CompanyConfiguration : IEntityTypeConfiguration<Company>
    {
        public void Configure(EntityTypeBuilder<Company> builder)
        {
            builder.ToTable("Companies");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.RowVersion).IsRowVersion();

            builder.HasOne(x => x.User)
                   .WithOne()
                   .HasForeignKey<CompanyUser>(x => x.CompanyId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
