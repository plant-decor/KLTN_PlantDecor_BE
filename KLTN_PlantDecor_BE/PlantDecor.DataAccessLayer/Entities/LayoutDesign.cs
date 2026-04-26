using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class LayoutDesign
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? PreviewImageUrl { get; set; }

    public string? RawResponse { get; set; }

    public int? Status { get; set; }

    public bool? IsSaved { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<AilayoutResponseModeration> AilayoutResponseModerations { get; set; } = new List<AilayoutResponseModeration>();

    public virtual ICollection<LayoutDesignAiResponseImage> LayoutDesignAiResponseImages { get; set; } = new List<LayoutDesignAiResponseImage>();

    public virtual ICollection<LayoutDesignPlant> LayoutDesignPlants { get; set; } = new List<LayoutDesignPlant>();

    public virtual ICollection<LayoutDesignRoomImage> LayoutDesignRoomImages { get; set; } = new List<LayoutDesignRoomImage>();

    public virtual User? User { get; set; }
}
