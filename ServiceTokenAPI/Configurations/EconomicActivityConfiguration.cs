using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceTokenApi.Entities;

namespace ServiceTokenApi.Configurations
{
    public class EconomicActivityDomainConfiguration : IEntityTypeConfiguration<EconomicActivityDomain>
    {
        public void Configure(EntityTypeBuilder<EconomicActivityDomain> builder)
        {
            builder.ToTable("EconomicActivityDomain");
            builder.HasKey(x => x.Id);
            builder.Property(e => e.Id).ValueGeneratedOnAdd();
            builder.Property(x => x.Name).IsRequired(true);
        }
    }
}