using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceTokenApi.Entities;

namespace ServiceTokenApi.Configurations
{
    public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
    {
        public void Configure(EntityTypeBuilder<Payment> builder)
        {
            builder.ToTable("Payments");
            builder.HasKey(x => x.Id);
            builder.Property(x => x.RowVersion).IsRowVersion();

            // The raw provider status keeps its original DB column name ("TbcStatus") so the
            // existing Payments table needs no schema migration after switching to Flitt.
            builder.Property(x => x.ProviderStatus).HasColumnName("TbcStatus");

            // Not unique: a single payment order (one order_id sent to Flitt) now spans several
            // per-token Payment rows, so multiple rows legitimately share a MerchantPaymentId.
            builder.HasIndex(x => x.MerchantPaymentId).IsUnique(false);
            builder.HasIndex(x => x.PayId).IsUnique(false);
            builder.HasIndex(x => x.ServiceTokenId).IsUnique(false);
            builder.HasIndex(x => x.InvestorPublicKey).IsUnique(false);
        }
    }
}
