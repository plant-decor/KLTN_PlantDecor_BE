using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class CommonPlant
{
    public int Id { get; set; }

    public int PlantId { get; set; }

    public int NurseryId { get; set; }

    public int Quantity { get; set; }

    public int ReservedQuantity { get; set; }

    public bool IsActive { get; set; }

    public virtual Plant Plant { get; set; } = null!;

    public virtual Nursery Nursery { get; set; } = null!;

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<NurseryOrderDetail> NurseryOrderDetails { get; set; } = new List<NurseryOrderDetail>();
}
