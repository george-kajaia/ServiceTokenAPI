namespace ServiceTokenApi.Dto
{
    /// <summary>Returned to the client after a payment is initiated so it can redirect to TBC.</summary>
    public class InitiatePaymentResultDto
    {
        public string PayId { get; set; } = null!;
        public string ApprovalUrl { get; set; } = null!;
    }
}
