namespace ServiceTokenApi.Dto
{
    /// <summary>
    /// Status of a (possibly multi-token) payment order, returned by
    /// <c>GET /api/Payment/GetStatus</c>. Because one Flitt order can now cover several tokens,
    /// the response carries an overall headline <see cref="Status"/>, a <see cref="Final"/> flag
    /// (true only once every token has reached a terminal state — the signal the client polls on),
    /// and the per-token breakdown in <see cref="Items"/> used to render the result list.
    /// </summary>
    public class PaymentBatchStatusDto
    {
        public string OrderId { get; set; } = null!;

        /// <summary>Overall headline status (e.g. Processing while any item is non-final).</summary>
        public string Status { get; set; } = null!;

        /// <summary>True once every token in the order has reached a terminal status.</summary>
        public bool Final { get; set; }

        /// <summary>Sum of the per-token amounts in this order.</summary>
        public decimal Amount { get; set; }

        public string Currency { get; set; } = "GEL";

        /// <summary>Per-token outcome, one entry per Payment row sharing this order id.</summary>
        public List<PaymentItemStatusDto> Items { get; set; } = new();
    }

    /// <summary>The outcome of a single token within a payment order.</summary>
    public class PaymentItemStatusDto
    {
        public string ServiceTokenId { get; set; } = null!;
        public string Status { get; set; } = null!;
    }
}
