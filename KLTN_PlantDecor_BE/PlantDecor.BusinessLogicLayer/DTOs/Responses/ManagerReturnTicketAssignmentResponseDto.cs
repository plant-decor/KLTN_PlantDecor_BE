namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class ManagerReturnTicketAssignmentResponseDto
    {
        public int AssignmentId { get; set; }
        public int ReturnTicketId { get; set; }
        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public int AssignmentStatus { get; set; }
        public string? AssignmentStatusName { get; set; }
        public DateTime? AssignedAt { get; set; }
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? TicketReason { get; set; }
        public int TicketStatus { get; set; }
        public string? TicketStatusName { get; set; }
        public decimal? TicketTotalRefundedAmount { get; set; }
        public List<ReturnTicketItemResponseDto> Items { get; set; } = new();
    }
}
