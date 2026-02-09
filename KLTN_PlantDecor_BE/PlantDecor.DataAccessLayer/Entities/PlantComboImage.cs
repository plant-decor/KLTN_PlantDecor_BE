using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class PlantComboImage
{
    public int Id { get; set; }

    public int? PlantComboId { get; set; }

    public string? ImageUrl { get; set; }

    public bool? IsPrimary { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual PlantCombo? PlantCombo { get; set; }
}
