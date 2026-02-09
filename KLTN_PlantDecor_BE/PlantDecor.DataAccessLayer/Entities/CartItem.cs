using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class CartItem
{
    public int Id { get; set; }

    public int? CartId { get; set; }

    public int? PlantId { get; set; }

    public int? PlantInstanceId { get; set; }

    public int? PlantComboId { get; set; }

    public int? InventoryId { get; set; }

    public int? Quantity { get; set; }

    public decimal? Price { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Cart? Cart { get; set; }

    public virtual Inventory? Inventory { get; set; }

    public virtual Plant? Plant { get; set; }

    public virtual PlantCombo? PlantCombo { get; set; }

    public virtual PlantInstance? PlantInstance { get; set; }
}
