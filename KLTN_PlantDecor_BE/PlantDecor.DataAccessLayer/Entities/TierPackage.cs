namespace PlantDecor.DataAccessLayer.Entities;

public class TierPackage
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    public int QuotaRequests { get; set; }
    public int? DurationMonths { get; set; }
    public bool IsActive { get; set; }
    public string? Description { get; set; }
    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();
}
