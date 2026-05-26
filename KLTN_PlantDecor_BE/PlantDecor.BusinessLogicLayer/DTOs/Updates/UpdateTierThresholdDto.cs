namespace PlantDecor.BusinessLogicLayer.DTOs.Updates
{
    public class UpdateTierThresholdDto
    {
        public string? Name { get; set; }
        public int? TierLevel { get; set; }
        public decimal? MinTotalSpent { get; set; }
        public string? BenefitDescription { get; set; }
        public bool? IsActive { get; set; }
        public int? MonthlyFreeQuota { get; set; }
    }
}
