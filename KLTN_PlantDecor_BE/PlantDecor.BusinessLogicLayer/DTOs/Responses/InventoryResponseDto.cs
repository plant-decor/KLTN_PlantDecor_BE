namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class InventoryResponseDto
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

        // Related data
        public List<CategoryResponseDto> Categories { get; set; } = new List<CategoryResponseDto>();
        public List<TagResponseDto> Tags { get; set; } = new List<TagResponseDto>();
        public List<InventoryImageResponseDto> Images { get; set; } = new List<InventoryImageResponseDto>();
    }

    public class InventoryImageResponseDto
    {
        public int Id { get; set; }
        public string? ImageUrl { get; set; }
        public bool? IsPrimary { get; set; }
    }

    public class InventoryListResponseDto
    {
        public int Id { get; set; }
        public string? InventoryCode { get; set; }
        public string? Name { get; set; }
        public decimal? BasePrice { get; set; }
        public int? StockQuantity { get; set; }
        public string? Unit { get; set; }
        public string? Brand { get; set; }
        public bool? IsActive { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public List<string> CategoryNames { get; set; } = new List<string>();
        public List<string> TagNames { get; set; } = new List<string>();
    }
}
