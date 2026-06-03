using System.ComponentModel.DataAnnotations;
using ServiceTokenApi.Enums;

namespace ServiceTokenApi.Entities
{
    /// <summary>
    /// Records a single TBC E-Commerce payment intent and links it back to the service
    /// token being purchased and the buyer. The bank callback uses <see cref="PayId"/>
    /// to find this row and finalize the purchase.
    /// </summary>
    public class Payment
    {
        public long Id { get; set; }

        [Timestamp]
        public uint RowVersion { get; set; }

        /// <summary>Our own identifier, sent to TBC as merchantPaymentId.</summary>
        public string MerchantPaymentId { get; set; } = null!;

        /// <summary>TBC-side payment id (payId). Null until the create-payment call returns.</summary>
        public string? PayId { get; set; }

        /// <summary>The service token this payment buys.</summary>
        public string ServiceTokenId { get; set; } = null!;

        /// <summary>Buyer (investor) public key.</summary>
        public string InvestorPublicKey { get; set; } = null!;

        public decimal Amount { get; set; }

        public string Currency { get; set; } = "GEL";

        public PaymentStatus Status { get; set; }

        /// <summary>Raw status string last reported by TBC, kept for diagnostics.</summary>
        public string? TbcStatus { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
