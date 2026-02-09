using System;
using System.Collections.Generic;

namespace PlantDecor.DataAccessLayer.Entities;

public partial class Inventory
{
    public int Id { get; set; }

    public string? InventoryCode { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public decimal? BasePrice { get; set; }

    public int? StockQuantity { get; set; }

    public string? Unit { get; set; }

    public string? Brand { get; set; }

    public string? Specifications { get; set; }

    public int? ExpiryMonths { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();

    public virtual ICollection<InventoryImage> InventoryImages { get; set; } = new List<InventoryImage>();

    public virtual ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public virtual ICollection<Category> Categories { get; set; } = new List<Category>();

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
