namespace PlantDecor.DataAccessLayer.Entities;

public class UserSubscription
{
    public int Id { get; set; }
    public int? UserId { get; set; }
    public int? TierPackageId { get; set; }
    public int? InvoiceId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsActive { get; set; }
    public bool IsMonthlyFree { get; set; }
    public int? QuotaOverride { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? CreatedAt { get; set; }

    public virtual User? User { get; set; }
    public virtual TierPackage? TierPackage { get; set; }
    public virtual Invoice? Invoice { get; set; }
    public virtual ICollection<UserAIUsage> UserAIUsages { get; set; } = new List<UserAIUsage>();
}
