using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class UserBehaviorLog
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? PlantId { get; set; }

    public int? PlantComboId { get; set; }

    public int? ActionType { get; set; }

    public string? Metadata { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Plant? Plant { get; set; }

    public virtual PlantCombo? PlantCombo { get; set; }

    public virtual User? User { get; set; }
}
