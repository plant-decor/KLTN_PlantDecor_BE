namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CreateReturnTicketRequestDto
    {
        public int OrderId { get; set; }
        public string? Reason { get; set; }
        public List<CreateReturnTicketItemRequestDto> Items { get; set; } = new();
    }

    public class CreateWholeOrderReturnTicketRequestDto
    {
        public int OrderId { get; set; }
        public string? Reason { get; set; }
    }

    public class CreateNurseryOrderReturnTicketRequestDto
    {
        public int NurseryOrderId { get; set; }
        public string? Reason { get; set; }
    }

    public class CreateReturnTicketItemRequestDto
    {
        public int NurseryOrderDetailId { get; set; }
        public int RequestedQuantity { get; set; }
        public string? Reason { get; set; }
    }
}
