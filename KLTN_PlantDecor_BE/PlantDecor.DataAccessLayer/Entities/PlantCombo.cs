using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class PlantCombo
{
    public int Id { get; set; }

    public string? ComboCode { get; set; }

    public string? ComboName { get; set; }

    public int? ComboType { get; set; }

    public string? Description { get; set; }

    public string? SuitableSpace { get; set; }

    public List<string>? SuitableRooms { get; set; }

    public int? FengShuiElement { get; set; }

    public string? FengShuiPurpose { get; set; }

    public bool? PetSafe { get; set; }

    public bool? ChildSafe { get; set; }

    public string? ThemeName { get; set; }

    public string? ThemeDescription { get; set; }

    public decimal? ComboPrice { get; set; }

    public int? Season { get; set; }

    public bool? IsActive { get; set; }

    public int? ViewCount { get; set; }

    public int? PurchaseCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<PlantComboImage> PlantComboImages { get; set; } = new List<PlantComboImage>();

    public virtual ICollection<PlantComboItem> PlantComboItems { get; set; } = new List<PlantComboItem>();

    public virtual ICollection<UserBehaviorLog> UserBehaviorLogs { get; set; } = new List<UserBehaviorLog>();

    public virtual ICollection<Tag> TagsNavigation { get; set; } = new List<Tag>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();

    public virtual ICollection<NurseryPlantCombo> NurseryPlantCombos { get; set; } = new List<NurseryPlantCombo>();
}
