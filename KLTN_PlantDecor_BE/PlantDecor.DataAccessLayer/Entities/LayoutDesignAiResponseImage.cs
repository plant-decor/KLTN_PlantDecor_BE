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

    public int? SourceType { get; set; }

    public string? ManualLayerJson { get; set; }

    public int? ManualEditedBy { get; set; }

    public DateTime? ManualEditedAt { get; set; }

    public int? ReplacesImageId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual LayoutDesign LayoutDesign { get; set; } = null!;

    public virtual LayoutDesignPlant? LayoutDesignPlant { get; set; }
}