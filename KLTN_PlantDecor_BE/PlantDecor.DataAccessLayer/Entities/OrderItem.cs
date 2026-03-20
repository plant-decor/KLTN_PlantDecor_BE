using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class OrderItem
{
    public int Id { get; set; }

    public int? OrderId { get; set; }

    // Foreign Keys for different product types (only one should be set)
    public int? CommonPlantId { get; set; }

    public int? PlantInstanceId { get; set; }

    public int? NurseryPlantComboId { get; set; }

    public int? NurseryMaterialId { get; set; }

    public string? ItemName { get; set; }

    public int? Quantity { get; set; }

    public decimal? Price { get; set; }

    public int? Status { get; set; }

    // Navigation Properties
    public virtual Order? Order { get; set; }

    public virtual CommonPlant? CommonPlant { get; set; }

    public virtual PlantInstance? PlantInstance { get; set; }

    public virtual NurseryPlantCombo? NurseryPlantCombo { get; set; }

    public virtual NurseryMaterial? NurseryMaterial { get; set; }
}
