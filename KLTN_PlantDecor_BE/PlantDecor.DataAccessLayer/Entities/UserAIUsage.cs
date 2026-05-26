namespace PlantDecor.DataAccessLayer.Entities;

public class UserAIUsage
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int UserSubscriptionId { get; set; }
    public int EndpointType { get; set; } // AIEndpointTypeEnum
    public int RequestCount { get; set; } = 1;
    public int? OrderId { get; set; }
    public string? ReferenceId { get; set; }
    public bool IsSuccess { get; set; }
    public bool IsRefunded { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
    public virtual UserSubscription UserSubscription { get; set; } = null!;
}
