namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class StaffScheduleResponseDto
    {
        public List<StaffScheduleItemResponseDto> Items { get; set; } = new();
    }

    public class StaffScheduleItemResponseDto
    {
        public int Id { get; set; }
        public string? TaskType { get; set; } // "CareService" hoặc "DesignService"
        public DateOnly? Date { get; set; }
        public ShiftInfoDto? Shift { get; set; }
        public UserSummaryDto? Customer { get; set; }
        public ServicePackageBriefDto? ServicePackage { get; set; }
        public string? Status { get; set; }
    }

    public class ShiftInfoDto
    {
        public TimeOnly? StartTime { get; set; }
        public TimeOnly? EndTime { get; set; }
    }

    public class ServicePackageBriefDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }
}