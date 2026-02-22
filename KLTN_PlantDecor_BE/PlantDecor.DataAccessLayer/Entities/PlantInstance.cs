using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class PlantInstance
{
    public int Id { get; set; }

    public int? PlantId { get; set; }

    public int? CurrentNurseryId { get; set; }

    public string? SKU { get; set; }

    public decimal? SpecificPrice { get; set; }

    public decimal? Height { get; set; }

    public decimal? TrunkDiameter { get; set; }

    public string? HealthStatus { get; set; }

    public int? Age { get; set; }

    public string? Description { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual Plant? Plant { get; set; }

    public virtual Nursery? CurrentNursery { get; set; }

    public virtual ICollection<PlantImage> PlantImages { get; set; } = new List<PlantImage>();

    public virtual ICollection<PlantRating> PlantRatings { get; set; } = new List<PlantRating>();

    public virtual UserPlant? UserPlant { get; set; }
}
