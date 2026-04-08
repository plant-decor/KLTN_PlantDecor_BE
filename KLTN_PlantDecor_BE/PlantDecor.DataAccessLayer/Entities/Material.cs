using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class Material
{
    public int Id { get; set; }

    public string? MaterialCode { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public decimal? BasePrice { get; set; }

    public string? Unit { get; set; }

    public string? Brand { get; set; }

    public string? Specifications { get; set; }

    public int? ExpiryMonths { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<MaterialImage> MaterialImages { get; set; } = new List<MaterialImage>();

    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();

    public virtual ICollection<NurseryMaterial> NurseryMaterials { get; set; } = new List<NurseryMaterial>();

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
