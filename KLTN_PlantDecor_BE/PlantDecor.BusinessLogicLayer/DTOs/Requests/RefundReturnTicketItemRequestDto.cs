namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class RefundReturnTicketItemRequestDto
    {
        public decimal? RefundedAmount { get; set; }
        public string? RefundReference { get; set; }
        public string? Note { get; set; }
    }
}
