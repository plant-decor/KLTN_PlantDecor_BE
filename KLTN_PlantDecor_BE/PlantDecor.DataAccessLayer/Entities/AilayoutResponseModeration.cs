using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class AilayoutResponseModeration
{
    public int Id { get; set; }

    public int? LayoutDesignId { get; set; }

    public int? Status { get; set; }

    public string? Reason { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public virtual LayoutDesign? LayoutDesign { get; set; }
}
