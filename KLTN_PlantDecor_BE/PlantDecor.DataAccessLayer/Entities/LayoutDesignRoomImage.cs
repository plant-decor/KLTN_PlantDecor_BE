namespace PlantDecor.DataAccessLayer.Entities;

public partial class LayoutDesignRoomImage
{
    public int LayoutDesignId { get; set; }

    public int RoomImageId { get; set; }

    public int? ViewAngle { get; set; }

    public int? OrderIndex { get; set; }

    public virtual LayoutDesign LayoutDesign { get; set; } = null!;

    public virtual RoomImage RoomImage { get; set; } = null!;
}