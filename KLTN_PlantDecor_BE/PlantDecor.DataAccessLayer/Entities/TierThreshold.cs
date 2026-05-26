namespace PlantDecor.DataAccessLayer.Entities;

public class TierThreshold
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int TierLevel { get; set; }
    public decimal MinTotalSpent { get; set; }
    public string? BenefitDescription { get; set; }
    public bool IsActive { get; set; }
}
