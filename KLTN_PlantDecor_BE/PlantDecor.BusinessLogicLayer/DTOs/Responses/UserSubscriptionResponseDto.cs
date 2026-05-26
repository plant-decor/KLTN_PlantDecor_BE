namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class UserSubscriptionResponseDto
    {
        public int Id { get; set; }
        public string PackageName { get; set; } = null!;
        public int TotalQuota { get; set; }
        public int UsedQuota { get; set; }
        public int RemainingQuota { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsMonthlyFree { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
