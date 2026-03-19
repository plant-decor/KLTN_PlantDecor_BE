using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class NurseryOrderDetail
{
    public int Id { get; set; }

    public int NurseryOrderId { get; set; }

    // Foreign keys for product sources. Exactly one should be set.
    public int? CommonPlantId { get; set; }

    public int? PlantInstanceId { get; set; }

    public int? NurseryPlantComboId { get; set; }

    public int? NurseryMaterialId { get; set; }

    public string? ItemName { get; set; }

    public int? Quantity { get; set; }

    public decimal? UnitPrice { get; set; }

    public decimal? Amount { get; set; }

    public int? Status { get; set; }

    public virtual NurseryOrder NurseryOrder { get; set; } = null!;

    public virtual CommonPlant? CommonPlant { get; set; }

    public virtual PlantInstance? PlantInstance { get; set; }

    public virtual NurseryPlantCombo? NurseryPlantCombo { get; set; }

    public virtual NurseryMaterial? NurseryMaterial { get; set; }
}