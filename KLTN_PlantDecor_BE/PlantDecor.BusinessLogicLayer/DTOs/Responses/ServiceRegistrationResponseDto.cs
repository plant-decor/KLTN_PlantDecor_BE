namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class ServiceRegistrationResponseDto
    {
        public int Id { get; set; }
        public int? Status { get; set; }
        public string? StatusName { get; set; }
        public DateOnly? ServiceDate { get; set; }
        public int? TotalSessions { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? Note { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public string? ScheduleDaysOfWeek { get; set; }
        public string? CancelReason { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public int? OrderId { get; set; }

        public NurseryCareServiceSummaryDto? NurseryCareService { get; set; }
        public ShiftSummaryDto? PrefferedShift { get; set; }
        public UserSummaryDto? Customer { get; set; }
        public UserSummaryDto? MainCaretaker { get; set; }
        public UserSummaryDto? CurrentCaretaker { get; set; }
        public List<ServiceProgressResponseDto> Progresses { get; set; } = new();
        public ServiceRatingResponseDto? Rating { get; set; }
    }

    public class NurseryCareServiceSummaryDto
    {
        public int Id { get; set; }
        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
        public CareServicePackageSummaryDto? CareServicePackage { get; set; }
    }

    public class CareServicePackageSummaryDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? VisitPerWeek { get; set; }
        public int? DurationDays { get; set; }
        public int? ServiceType { get; set; }
        public decimal? UnitPrice { get; set; }
    }

    public class ShiftSummaryDto
    {
        public int Id { get; set; }
        public string? ShiftName { get; set; }
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
    }

    public class UserSummaryDto
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Avatar { get; set; }
    }
}
