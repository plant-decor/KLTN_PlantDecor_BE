using System;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class LayoutDesignAiResponseImage
{
    public int Id { get; set; }

    public int LayoutDesignId { get; set; }

    public int? LayoutDesignPlantId { get; set; }

    public string? ImageUrl { get; set; }

    public string? PublicId { get; set; }

    public string? FluxPromptUsed { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual LayoutDesign LayoutDesign { get; set; } = null!;

    public virtual LayoutDesignPlant? LayoutDesignPlant { get; set; }
}