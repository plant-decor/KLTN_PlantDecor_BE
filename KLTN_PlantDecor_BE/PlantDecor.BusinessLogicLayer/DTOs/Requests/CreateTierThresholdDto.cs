namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CreateTierThresholdDto
    {
        public string Name { get; set; } = null!;
        public int TierLevel { get; set; }
        public decimal MinTotalSpent { get; set; }
        public string? BenefitDescription { get; set; }
        public int MonthlyFreeQuota { get; set; } = 0;
    }
}
