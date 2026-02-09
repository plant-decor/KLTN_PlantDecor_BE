using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class OrderItem
{
    public int Id { get; set; }

    public int? OrderId { get; set; }

    public int? PlantId { get; set; }

    public int? PlantInstanceId { get; set; }

    public int? PlantComboId { get; set; }

    public int? InventoryId { get; set; }

    public int? ServiceId { get; set; }

    public string? ItemName { get; set; }

    public int? Quantity { get; set; }

    public decimal? Price { get; set; }

    public virtual Inventory? Inventory { get; set; }

    public virtual Order? Order { get; set; }

    public virtual Plant? Plant { get; set; }

    public virtual PlantCombo? PlantCombo { get; set; }

    public virtual PlantInstance? PlantInstance { get; set; }
}
