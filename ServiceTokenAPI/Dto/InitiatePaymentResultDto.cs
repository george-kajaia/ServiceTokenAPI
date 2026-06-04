namespace ServiceTokenApi.Dto
{
    /// <summary>
    /// Returned to the client after a payment is initiated. The frontend feeds <see cref="Token"/>
    /// to the Flitt embedded checkout widget and polls status with <see cref="OrderId"/>.
    /// </summary>
    public class InitiatePaymentResultDto
    {
        public string OrderId { get; set; } = null!;
        public string Token { get; set; } = null!;
    }
}
