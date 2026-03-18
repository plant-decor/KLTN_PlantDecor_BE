namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CreatePaymentRequestDto
    {
        public int? OrderId { get; set; }
        public string? OrderGroupCode { get; set; }
    }
}
