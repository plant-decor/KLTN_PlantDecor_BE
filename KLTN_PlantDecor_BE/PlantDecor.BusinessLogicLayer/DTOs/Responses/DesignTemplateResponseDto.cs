namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class DesignTemplateResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? Style { get; set; }
        public List<int>? RoomTypes { get; set; }
        public string? ImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<SpecializationSummaryDto> Specializations { get; set; } = new();
        public List<DesignTemplateTierResponseDto> Tiers { get; set; } = new();
        public List<DesignTemplateNurserySummaryDto> NurseryOfferings { get; set; } = new();
    }

    public class DesignTemplateOptionResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? ImageUrl { get; set; }
    }

    public class DesignTemplateNurserySummaryDto
    {
        public int NurseryDesignTemplateId { get; set; }
        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
        public bool IsActive { get; set; }
    }
}
