using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class PlantRating
{
    public int Id { get; set; }

    public int? PlantId { get; set; }

    public int? UserId { get; set; }

    public int? PlantInstanceId { get; set; }

    public decimal? Rating { get; set; }

    public string? Description { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Plant? Plant { get; set; }

    public virtual PlantInstance? PlantInstance { get; set; }

    public virtual User? User { get; set; }
}
