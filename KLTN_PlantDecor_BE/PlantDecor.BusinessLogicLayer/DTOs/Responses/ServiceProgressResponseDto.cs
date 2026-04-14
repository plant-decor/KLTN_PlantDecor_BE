namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class ServiceProgressResponseDto
    {
        public int Id { get; set; }
        public int? ServiceRegistrationId { get; set; }
        public int? Status { get; set; }
        public string? StatusName { get; set; }
        public DateOnly? TaskDate { get; set; }
        public DateTime? ActualStartTime { get; set; }
        public DateTime? ActualEndTime { get; set; }
        public string? Description { get; set; }
        public string? EvidenceImageUrl { get; set; }

        public ShiftSummaryDto? Shift { get; set; }
        public UserSummaryDto? Caretaker { get; set; }
        public ServiceRegistrationBriefDto? ServiceRegistration { get; set; }
    }

    public class ServiceRegistrationBriefDto
    {
        public int Id { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public NurseryCareServiceSummaryDto? NurseryCareService { get; set; }
        public UserSummaryDto? Customer { get; set; }
    }
}
