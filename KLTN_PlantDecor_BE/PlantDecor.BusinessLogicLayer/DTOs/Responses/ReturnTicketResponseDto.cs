namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class ReturnTicketResponseDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int CustomerId { get; set; }
        public string? Reason { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public decimal? TotalRefundedAmount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<ReturnTicketItemResponseDto> Items { get; set; } = new();
        public List<ReturnTicketAssignmentResponseDto> Assignments { get; set; } = new();
    }

    public class ReturnTicketItemResponseDto
    {
        public int Id { get; set; }
        public int NurseryOrderDetailId { get; set; }
        public string? ItemName { get; set; }
        public string? ProductImageUrl { get; set; }
        public int RequestedQuantity { get; set; }
        public int? ApprovedQuantity { get; set; }
        public string? Reason { get; set; }
        public string? ManagerDecisionNote { get; set; }
        public decimal? RefundedAmount { get; set; }
        public string? RefundReference { get; set; }
        public DateTime? RefundedAt { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public int? NurseryOrderId { get; set; }
        public int? NurseryId { get; set; }
        public List<string> ImageUrls { get; set; } = new();
    }

    public class ReturnTicketAssignmentResponseDto
    {
        public int Id { get; set; }
        public int NurseryId { get; set; }
        public int? ManagerId { get; set; }
        public string? ManagerName { get; set; }
        public int Status { get; set; }
        public string? StatusName { get; set; }
        public DateTime? AssignedAt { get; set; }
    }
}
