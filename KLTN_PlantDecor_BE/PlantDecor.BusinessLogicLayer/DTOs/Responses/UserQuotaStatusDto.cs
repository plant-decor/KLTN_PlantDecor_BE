namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class UserQuotaStatusDto
    {
        public int TierLevel { get; set; }
        public string? TierName { get; set; }
        public int TierMonthlyFreeQuota { get; set; }
        public int TotalRemainingQuota { get; set; }
        public List<SubscriptionQuotaDto> ActiveSubscriptions { get; set; } = new();
    }

    public class SubscriptionQuotaDto
    {
        public int SubscriptionId { get; set; }
        public string PackageName { get; set; } = null!;
        public int TotalQuota { get; set; }
        public int UsedQuota { get; set; }
        public int RemainingQuota { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsMonthlyFree { get; set; }
    }
}
