namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class DesignRegistrationResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int? OrderId { get; set; }
        public int NurseryId { get; set; }
        public int DesignTemplateTierId { get; set; }
        public int? AssignedCaretakerId { get; set; }
        public decimal TotalPrice { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public decimal? Width { get; set; }
        public decimal? Length { get; set; }
        public string? CurrentStateImageUrl { get; set; }
        public string Address { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CustomerNote { get; set; }
        public string? CancelReason { get; set; }
        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public DateTime? CreatedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public UserSummaryDto? Customer { get; set; }
        public UserSummaryDto? AssignedCaretaker { get; set; }
        public DesignNurserySummaryDto? Nursery { get; set; }
        public DesignTemplateTierSummaryDto? DesignTemplateTier { get; set; }
        public List<DesignTaskResponseDto> DesignTasks { get; set; } = new();
    }

    public class DesignNurserySummaryDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class DesignTemplateTierSummaryDto
    {
        public int Id { get; set; }
        public string? TierName { get; set; }
        public decimal MinArea { get; set; }
        public decimal MaxArea { get; set; }
        public decimal PackagePrice { get; set; }
        public int EstimatedDays { get; set; }
        public string? ScopedOfWork { get; set; }
        public DesignTemplateSummaryDto? DesignTemplate { get; set; }
    }

    public class DesignTemplateSummaryDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
        public int? Style { get; set; }
        public List<int>? RoomTypes { get; set; }
    }
}
