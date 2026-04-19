namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class DesignTaskResponseDto
    {
        public int Id { get; set; }
        public int DesignRegistrationId { get; set; }
        public int? AssignedStaffId { get; set; }
        public DateOnly? ScheduledDate { get; set; }
        public int TaskType { get; set; }
        public string TaskTypeName { get; set; } = string.Empty;
        public string? ReportImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public UserSummaryDto? AssignedStaff { get; set; }
        public DesignRegistrationTaskSummaryDto? Registration { get; set; }
        public List<TaskMaterialUsageResponseDto> TaskMaterialUsages { get; set; } = new();
    }

    public class DesignRegistrationTaskSummaryDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? AssignedCaretakerId { get; set; }
        public int NurseryId { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Phone { get; set; }
    }

    public class TaskMaterialUsageResponseDto
    {
        public int Id { get; set; }
        public int MaterialId { get; set; }
        public string? MaterialName { get; set; }
        public decimal? ActualQuantity { get; set; }
        public string? Note { get; set; }
    }

    public class DesignTaskPackageMaterialResponseDto
    {
        public int MaterialId { get; set; }
        public string? MaterialName { get; set; }
        public decimal SuggestedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public bool IsAvailableInNursery { get; set; }
        public bool IsActiveInNursery { get; set; }
    }
}
