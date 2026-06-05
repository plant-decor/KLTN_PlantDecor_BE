namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class ApproveReturnTicketAssignmentRequestDto
    {
        public string? Note { get; set; }
    }

    public class RejectReturnTicketAssignmentRequestDto
    {
        public string? Note { get; set; }
    }

    public class RefundReturnTicketAssignmentRequestDto
    {
        public string? RefundReference { get; set; }
        public string? Note { get; set; }
    }
}
