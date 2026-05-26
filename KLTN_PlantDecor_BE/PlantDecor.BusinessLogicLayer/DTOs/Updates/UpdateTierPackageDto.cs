namespace PlantDecor.BusinessLogicLayer.DTOs.Updates
{
    public class UpdateTierPackageDto
    {
        public string? Name { get; set; }
        public decimal? Price { get; set; }
        public int? QuotaRequests { get; set; }
        public int? DurationMonths { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }
}
