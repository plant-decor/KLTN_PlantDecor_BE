namespace PlantDecor.DataAccessLayer.Entities;

public partial class RoomImage
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string? ImageUrl { get; set; }

    public int? ViewAngle { get; set; }

    public DateTime? UploadedAt { get; set; }

    public virtual ICollection<LayoutDesignRoomImage> LayoutDesignRoomImages { get; set; } = new List<LayoutDesignRoomImage>();

    public virtual ICollection<RoomUploadModeration> RoomUploadModerations { get; set; } = new List<RoomUploadModeration>();

    public virtual ICollection<RoomDesignPreferences> RoomDesignPreferences { get; set; } = new List<RoomDesignPreferences>();

    public virtual User? User { get; set; }
}
