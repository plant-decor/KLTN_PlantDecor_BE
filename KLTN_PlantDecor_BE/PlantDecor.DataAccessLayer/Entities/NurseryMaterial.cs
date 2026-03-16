using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class NurseryMaterial
{
    public int Id { get; set; }

    public int MaterialId { get; set; }

    public int NurseryId { get; set; }

    public int Quantity { get; set; }

    public int ReservedQuantity { get; set; }

    public DateOnly? ExpiredDate { get; set; }

    public bool IsActive { get; set; }

    public virtual Material Material { get; set; } = null!;

    public virtual Nursery Nursery { get; set; } = null!;

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
