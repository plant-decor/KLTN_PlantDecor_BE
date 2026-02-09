using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class UserPreference
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? PlantId { get; set; }

    public decimal? PreferenceScore { get; set; }

    public decimal? ProfileMatchScore { get; set; }

    public decimal? BehaviorScore { get; set; }

    public decimal? PurchaseHistoryScore { get; set; }

    public DateTime? LastCalculated { get; set; }

    public virtual Plant? Plant { get; set; }

    public virtual User? User { get; set; }
}
