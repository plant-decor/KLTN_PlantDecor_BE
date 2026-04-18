namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class CareServicePackageWithNurseriesResponseDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Features { get; set; }
        public int? VisitPerWeek { get; set; }
        public int? DurationDays { get; set; }
        public int? TotalSessions { get; set; }
        public int? ServiceType { get; set; }
        public int? AreaLimit { get; set; }
        public decimal? UnitPrice { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
        public List<SpecializationSummaryDto> Specializations { get; set; } = new();
        public List<NurseryCareServiceOptionResponseDto> NurseryCareServices { get; set; } = new();
    }

    public class NurseryCareServiceOptionResponseDto
    {
        public int NurseryCareServiceId { get; set; }
        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
    }
}
