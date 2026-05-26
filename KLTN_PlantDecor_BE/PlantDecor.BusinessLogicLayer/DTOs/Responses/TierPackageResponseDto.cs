namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class TierPackageResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal Price { get; set; }
        public int QuotaRequests { get; set; }
        public int? DurationMonths { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
