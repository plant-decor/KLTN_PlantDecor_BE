using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class Plant
{
    public int Id { get; set; }

    public string? PlantCode { get; set; }

    public string Name { get; set; } = null!;

    public string? SpecificName { get; set; }

    public string? Origin { get; set; }

    public string? Description { get; set; }

    public decimal? BasePrice { get; set; }

    public string? Placement { get; set; }

    public string? Size { get; set; }

    public int? MinHeight { get; set; }

    public int? MaxHeight { get; set; }

    public string? GrowthRate { get; set; }

    public bool? Toxicity { get; set; }

    public bool? AirPurifying { get; set; }

    public bool? HasFlower { get; set; }

    public string? FengShuiElement { get; set; }

    public string? FengShuiMeaning { get; set; }

    public bool? PotIncluded { get; set; }

    public string? PotSize { get; set; }

    public string? PlantType { get; set; }

    public string? CareLevel { get; set; }

    public string? Texture { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<PlantComboItem> PlantComboItems { get; set; } = new List<PlantComboItem>();

    public virtual ICollection<PlantGuide> PlantGuides { get; set; } = new List<PlantGuide>();

    public virtual ICollection<PlantImage> PlantImages { get; set; } = new List<PlantImage>();

    public virtual ICollection<PlantInstance> PlantInstances { get; set; } = new List<PlantInstance>();

    public virtual ICollection<PlantRating> PlantRatings { get; set; } = new List<PlantRating>();

    public virtual ICollection<UserBehaviorLog> UserBehaviorLogs { get; set; } = new List<UserBehaviorLog>();

    public virtual ICollection<UserPlant> UserPlants { get; set; } = new List<UserPlant>();

    public virtual ICollection<UserPreference> UserPreferences { get; set; } = new List<UserPreference>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
