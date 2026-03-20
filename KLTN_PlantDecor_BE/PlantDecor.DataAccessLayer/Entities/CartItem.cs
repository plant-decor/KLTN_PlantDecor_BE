using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class CartItem
{
    public int Id { get; set; }

    public int? CartId { get; set; }

    // Foreign Keys for different product types (only one should be set)
    public int? CommonPlantId { get; set; }

    public int? NurseryPlantComboId { get; set; }

    public int? NurseryMaterialId { get; set; }

    public int? Quantity { get; set; }

    public decimal? Price { get; set; }

    public DateTime? CreatedAt { get; set; }

    // Navigation Properties
    public virtual Cart? Cart { get; set; }

    public virtual CommonPlant? CommonPlant { get; set; }

    public virtual NurseryPlantCombo? NurseryPlantCombo { get; set; }

    public virtual NurseryMaterial? NurseryMaterial { get; set; }
}
