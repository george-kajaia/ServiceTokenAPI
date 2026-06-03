using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceTokenApi.Entities;

namespace ServiceTokenApi.Configurations
{
    public class LegalFormDomainConfiguration : IEntityTypeConfiguration<LegalFormDomain>
    {
        public void Configure(EntityTypeBuilder<LegalFormDomain> builder)
        {
            builder.ToTable("LegalFormDomain");
            builder.HasKey(x => x.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(x => x.Name).IsRequired(true);
        }
    }
}