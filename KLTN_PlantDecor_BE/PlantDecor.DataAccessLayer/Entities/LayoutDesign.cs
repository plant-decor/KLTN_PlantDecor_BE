using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class LayoutDesign
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? PreviewImageUrl { get; set; }

    public int? ModerationStatus { get; set; }

    public string? AllowedToAll { get; set; }

    public bool? IsSaved { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<AilayoutResponseModeration> AilayoutResponseModerations { get; set; } = new List<AilayoutResponseModeration>();

    public virtual ICollection<RoomImage> RoomImages { get; set; } = new List<RoomImage>();

    public virtual User? User { get; set; }
}
