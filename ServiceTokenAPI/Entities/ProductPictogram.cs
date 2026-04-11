namespace ServiceTokenApi.Entities
{
    public class ProductPictogram
    {
        public long ProductId { get; set; }
        public byte[] Pictogram { get; set; } = Array.Empty<byte>();
    }
}
