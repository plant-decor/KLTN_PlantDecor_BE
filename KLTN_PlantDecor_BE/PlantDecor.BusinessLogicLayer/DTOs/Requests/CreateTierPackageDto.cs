namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CreateTierPackageDto
    {
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public int QuotaRequests { get; set; }
        public int? DurationMonths { get; set; }
        public string? Description { get; set; }
    }
}
