using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class UserPlant
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? PlantId { get; set; }

    public int? PlantInstanceId { get; set; }

    public DateOnly? PurchaseDate { get; set; }

    public DateOnly? LastWateredDate { get; set; }

    public DateOnly? LastFertilizedDate { get; set; }

    public DateOnly? LastPrunedDate { get; set; }

    public string? Location { get; set; }

    public decimal? CurrentTrunkDiameter { get; set; }

    public decimal? CurrentHeight { get; set; }

    public string? HealthStatus { get; set; }

    public int? Age { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CareReminder> CareReminders { get; set; } = new List<CareReminder>();

    public virtual Plant? Plant { get; set; }

    public virtual PlantInstance? PlantInstance { get; set; }

    public virtual User? User { get; set; }
}
