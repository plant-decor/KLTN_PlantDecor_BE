using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class Tag
{
    public int Id { get; set; }

    public string TagName { get; set; } = null!;

    public virtual ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();

    public virtual ICollection<PlantCombo> PlantCombos { get; set; } = new List<PlantCombo>();

    public virtual ICollection<Plant> Plants { get; set; } = new List<Plant>();
}
