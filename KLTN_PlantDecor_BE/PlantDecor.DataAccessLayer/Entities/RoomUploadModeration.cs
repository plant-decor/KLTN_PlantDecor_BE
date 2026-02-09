using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class RoomUploadModeration
{
    public int Id { get; set; }

    public int? RoomImageId { get; set; }

    public int? Status { get; set; }

    public string? Reason { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public virtual RoomImage? RoomImage { get; set; }
}
