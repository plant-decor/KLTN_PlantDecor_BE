using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class RoomImage
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public int? LayoutDesignId { get; set; }

    public string? ImageUrl { get; set; }

    public DateTime? UploadedAt { get; set; }

    public int? ViewAngle { get; set; }

    public virtual LayoutDesign? LayoutDesign { get; set; }

    public virtual ICollection<RoomUploadModeration> RoomUploadModerations { get; set; } = new List<RoomUploadModeration>();

    public virtual User? User { get; set; }
}
