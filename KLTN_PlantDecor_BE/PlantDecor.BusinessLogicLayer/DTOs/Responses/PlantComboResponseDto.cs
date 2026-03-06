namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class PlantComboResponseDto
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
        public decimal? ComboPrice { get; set; }
        public decimal? DiscountPercent { get; set; }
        public int? MinPlants { get; set; }
        public int? MaxPlants { get; set; }
        public string? Tags { get; set; }
        public string? Season { get; set; }
        public bool? IsActive { get; set; }
        public int? ViewCount { get; set; }
        public int? PurchaseCount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Related data
        public List<PlantComboItemResponseDto> ComboItems { get; set; } = new List<PlantComboItemResponseDto>();
        public List<PlantComboImageResponseDto> Images { get; set; } = new List<PlantComboImageResponseDto>();
        public List<TagResponseDto> TagsNavigation { get; set; } = new List<TagResponseDto>();
    }

    public class PlantComboItemResponseDto
    {
        public int Id { get; set; }
        public int? PlantComboId { get; set; }
        public int? PlantId { get; set; }
        public string? PlantName { get; set; }
        public int? Quantity { get; set; }
        public string? Notes { get; set; }
    }

    public class PlantComboImageResponseDto
    {
        public int Id { get; set; }
        public string? ImageUrl { get; set; }
        public bool? IsPrimary { get; set; }
    }

    public class PlantComboListResponseDto
    {
        public int Id { get; set; }
        public string? ComboCode { get; set; }
        public string? ComboName { get; set; }
        public int? ComboType { get; set; }
        public decimal? OriginalPrice { get; set; }
        public decimal? ComboPrice { get; set; }
        public decimal? DiscountPercent { get; set; }
        public int? Quantity { get; set; }
        public bool? IsActive { get; set; }
        public int? ViewCount { get; set; }
        public int? PurchaseCount { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public int TotalItems { get; set; }
        public List<string> TagNames { get; set; } = new List<string>();
    }
}
