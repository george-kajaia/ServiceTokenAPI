namespace ServiceTokenApi.Dto
{
    /// <summary>
    /// Body for <c>POST /api/Payment/InitiateEmbeddedPaymentBatch</c>: the full set of in-cart
    /// tokens the investor wants to buy together, plus the buyer's public key. A single Flitt
    /// order is created for the summed amount (the payment widget is shown once), while each
    /// token is still recorded as its own row in the Payments table.
    /// </summary>
    public class InitiatePaymentBatchRequestDto
    {
        /// <summary>The tokens to purchase in this one payment. Must all be InCart.</summary>
        public List<PaymentTokenRefDto> Tokens { get; set; } = new();

        /// <summary>Buyer (investor) public key — the owner assigned to every token on success.</summary>
        public string InvestorPublicKey { get; set; } = null!;
    }

    /// <summary>A single token reference (id + optimistic-concurrency row version).</summary>
    public class PaymentTokenRefDto
    {
        public string ServiceTokenId { get; set; } = null!;
        public uint RowVersion { get; set; }
    }
}
