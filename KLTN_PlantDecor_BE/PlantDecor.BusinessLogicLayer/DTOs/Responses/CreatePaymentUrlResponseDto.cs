namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class CreatePaymentUrlResponseDto
    {
        public int PaymentId { get; set; }
        public string PaymentUrl { get; set; } = string.Empty;
    }
}
