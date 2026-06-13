using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceTokenApi.Entities;

namespace ServiceTokenApi.Configurations
{
    public class CompanyUserConfiguration : IEntityTypeConfiguration<CompanyUser>
    {
        public void Configure(EntityTypeBuilder<CompanyUser> builder)
        {
            builder.ToTable("CompanyUsers");
            builder.HasKey(x => x.Id);
            builder.HasIndex(x => new { x.UserName, x.Password });
            builder.HasIndex(x => x.CompanyId);
            builder.Property(x => x.CompanyId).IsRequired();
            builder.Property(x => x.UserType).IsRequired();
            builder.HasOne<Company>()
                   .WithMany(c => c.Users)
                   .HasForeignKey(x => x.CompanyId)
                   .IsRequired()
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}