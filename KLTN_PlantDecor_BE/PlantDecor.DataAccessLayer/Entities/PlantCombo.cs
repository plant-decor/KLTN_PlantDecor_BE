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

    public string? SuitableRooms { get; set; }

    public string? FengShuiElement { get; set; }

    public string? FengShuiPurpose { get; set; }

    public string? ThemeName { get; set; }

    public string? ThemeDescription { get; set; }

    public decimal? OriginalPrice { get; set; }

    public decimal? SalePrice { get; set; }

    public decimal? DiscountPercent { get; set; }

    public int? MinPlants { get; set; }

    public int? MaxPlants { get; set; }

    public string? Tags { get; set; }

    public string? Season { get; set; }

    public int? Quantity { get; set; }

    public bool? IsActive { get; set; }

    public int? ViewCount { get; set; }

    public int? PurchaseCount { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<PlantComboImage> PlantComboImages { get; set; } = new List<PlantComboImage>();

    public virtual ICollection<PlantComboItem> PlantComboItems { get; set; } = new List<PlantComboItem>();

    public virtual ICollection<UserBehaviorLog> UserBehaviorLogs { get; set; } = new List<UserBehaviorLog>();

    public virtual ICollection<Tag> TagsNavigation { get; set; } = new List<Tag>();
}
