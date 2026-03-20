using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class Wishlist
{

    public int PlantId { get; set; }

    public int UserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Plant Plant { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
