using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class LayoutDesignPlant
{
    public int Id { get; set; }

    public int LayoutDesignId { get; set; }

    public int? CommonPlantId { get; set; }

    public int? PlantInstanceId { get; set; }

    public string? PlantReason { get; set; }

    public string? PlacementPosition { get; set; }

    public string? PlacementReason { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual LayoutDesign LayoutDesign { get; set; } = null!;

    public virtual ICollection<LayoutDesignAiResponseImage> LayoutDesignAiResponseImages { get; set; } = new List<LayoutDesignAiResponseImage>();

    public virtual CommonPlant? CommonPlant { get; set; }

    public virtual PlantInstance? PlantInstance { get; set; }
}
