using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class PlantInventory
{
    public int Id { get; set; }

    public int PlantId { get; set; }

    public int NurseryId { get; set; }

    public int Quantity { get; set; }

    public int ReservedQuantity { get; set; }

    public virtual Plant Plant { get; set; } = null!;

    public virtual Nursery Nursery { get; set; } = null!;
}
