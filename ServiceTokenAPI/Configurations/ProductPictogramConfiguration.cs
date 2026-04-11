using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceTokenApi.Entities;

namespace ServiceTokenApi.Configurations
{
    public class ProductPictogramConfiguration : IEntityTypeConfiguration<ProductPictogram>
    {
        public void Configure(EntityTypeBuilder<ProductPictogram> builder)
        {
            builder.ToTable("ProductPictograms");

            builder.HasKey(e => e.ProductId);

            builder.Property(e => e.Pictogram).HasColumnType("bytea").IsRequired();

            builder.HasOne<Product>()
                   .WithOne(p => p.ProductPictogram)
                   .HasForeignKey<ProductPictogram>(e => e.ProductId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
