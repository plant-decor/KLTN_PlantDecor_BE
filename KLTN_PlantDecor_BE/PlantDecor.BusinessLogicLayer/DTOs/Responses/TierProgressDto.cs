namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class TierProgressDto
    {
        public int CurrentTierLevel { get; set; }
        public string? CurrentTierName { get; set; }
        public string? CurrentTierBenefitDescription { get; set; }
        public int CurrentTierMonthlyFreeQuota { get; set; }
        public decimal CurrentTierMinSpent { get; set; }

        public decimal TotalSpent { get; set; }

        public int? NextTierLevel { get; set; }
        public string? NextTierName { get; set; }
        public string? NextTierBenefitDescription { get; set; }
        public int? NextTierMonthlyFreeQuota { get; set; }
        public decimal? NextTierMinSpent { get; set; }
        public decimal? AmountToNextTier { get; set; }

        public double ProgressPercent { get; set; }
        public bool IsMaxTier { get; set; }
    }
}
