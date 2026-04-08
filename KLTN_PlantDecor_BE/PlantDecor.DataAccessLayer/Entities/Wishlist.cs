using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class Wishlist
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public WishlistItemType ItemType { get; set; }

    // Nullable foreign keys for different item types
    public int? PlantId { get; set; }

    public int? PlantInstanceId { get; set; }

    public int? PlantComboId { get; set; }

    public int? MaterialId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;

    public virtual Plant? Plant { get; set; }

    public virtual PlantInstance? PlantInstance { get; set; }

    public virtual PlantCombo? PlantCombo { get; set; }

    public virtual Material? Material { get; set; }
}
