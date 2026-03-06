using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class NurseryPlantCombo
{
    public int Id { get; set; }

    public int PlantComboId { get; set; }

    public int NurseryId { get; set; }

    public int Quantity { get; set; }

    public bool IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual PlantCombo PlantCombo { get; set; } = null!;

    public virtual Nursery Nursery { get; set; } = null!;

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
