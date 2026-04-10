using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class LayoutDesignAiResponseImage
{
    public int Id { get; set; }

    public int LayoutDesignId { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual LayoutDesign LayoutDesign { get; set; } = null!;
}