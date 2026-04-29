using System;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class AIChatMessage
{
    public int Id { get; set; }

    public int AIChatSessionId { get; set; }

    public int? Role { get; set; }

    public string? Content { get; set; }

    public string? Intent { get; set; }

    public bool? IsFallback { get; set; }

    public bool? IsPolicyResponse { get; set; }

    // Persist assistant payload for later rendering in history (jsonb)
    public string? SuggestedPlants { get; set; }

    // Persist assistant payload for later rendering in history (jsonb)
    public string? CareTips { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual AIChatSession? AIChatSession { get; set; }
}