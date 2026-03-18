using System;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class Shipping
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public int? Status { get; set; }

    public string? TrackingCode { get; set; }

    public string? Note { get; set; }

    public DateTime? ShippedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Order Order { get; set; } = null!;
}