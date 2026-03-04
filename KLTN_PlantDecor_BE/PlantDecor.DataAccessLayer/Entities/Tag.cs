using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class Tag
{
    public int Id { get; set; }

    public string TagName { get; set; } = null!;

    public int TagType { get; set; }

    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();

    public virtual ICollection<PlantCombo> PlantCombos { get; set; } = new List<PlantCombo>();

    public virtual ICollection<Plant> Plants { get; set; } = new List<Plant>();
}
