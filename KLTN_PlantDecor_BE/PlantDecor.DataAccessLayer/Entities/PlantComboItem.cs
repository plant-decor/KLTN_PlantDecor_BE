using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class PlantComboItem
{
    public int Id { get; set; }

    public int? PlantComboId { get; set; }

    public int? PlantId { get; set; }

    public int? Quantity { get; set; }

    public string? Notes { get; set; }

    public virtual Plant? Plant { get; set; }

    public virtual PlantCombo? PlantCombo { get; set; }
}
