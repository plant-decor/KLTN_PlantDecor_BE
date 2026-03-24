using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class Wishlist
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public WishlistItemType ItemType { get; set; }

    // Nullable foreign keys for different item types
    public int? CommonPlantId { get; set; }

    public int? PlantInstanceId { get; set; }

    public int? NurseryPlantComboId { get; set; }

    public int? NurseryMaterialId { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;

    public virtual CommonPlant? CommonPlant { get; set; }

    public virtual PlantInstance? PlantInstance { get; set; }

    public virtual NurseryPlantCombo? NurseryPlantCombo { get; set; }

    public virtual NurseryMaterial? NurseryMaterial { get; set; }
}
