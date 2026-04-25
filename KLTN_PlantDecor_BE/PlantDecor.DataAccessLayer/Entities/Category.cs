using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class Category
{
    public int Id { get; set; }

    public int? ParentCategoryId { get; set; }

    public string Name { get; set; } = null!;

    public bool? IsActive { get; set; }

    public int CategoryType { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Category> InverseParentCategory { get; set; } = new List<Category>();

    public virtual Category? ParentCategory { get; set; }

    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();

    public virtual ICollection<Plant> Plants { get; set; } = new List<Plant>();
    public virtual ICollection<PackagePlantSuitability> PackagePlantSuitabilities { get; set; } = new List<PackagePlantSuitability>();
}
