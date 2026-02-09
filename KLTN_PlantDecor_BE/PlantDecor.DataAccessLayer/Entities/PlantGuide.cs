using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class PlantGuide
{
    public int Id { get; set; }

    public int? PlantId { get; set; }

    public int? LightRequirement { get; set; }

    public string? Watering { get; set; }

    public string? Fertilizing { get; set; }

    public string? Pruning { get; set; }

    public string? Temperature { get; set; }

    public string? CareNotes { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Plant? Plant { get; set; }
}
