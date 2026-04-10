namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class PlantComboResponseDto
    {
        public int Id { get; set; }
        public string? ComboCode { get; set; }
        public string? ComboName { get; set; }
        public int? ComboType { get; set; }
        public string? ComboTypeName { get; set; }
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
        public string? SeasonName { get; set; }
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
        public string? ComboTypeName { get; set; }
        public decimal? ComboPrice { get; set; }
        public int? Season { get; set; }
        public string? SeasonName { get; set; }
        public bool? PetSafe { get; set; }
        public bool? ChildSafe { get; set; }
        public bool? IsActive { get; set; }
        public int? ViewCount { get; set; }
        public int? PurchaseCount { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public int TotalItems { get; set; }
        public List<string> TagNames { get; set; } = new List<string>();
    }

    public class NurseryComboStockOperationResponseDto
    {
        public int NurseryId { get; set; }
        public int PlantComboId { get; set; }
        public string? ComboName { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public int QuantityProcessed { get; set; }
        public int ComboStockBefore { get; set; }
        public int ComboStockAfter { get; set; }
        public List<NurseryComboPlantStockChangeDto> PlantStockChanges { get; set; } = new List<NurseryComboPlantStockChangeDto>();
    }

    public class NurseryComboPlantStockChangeDto
    {
        public int PlantId { get; set; }
        public string PlantName { get; set; } = string.Empty;
        public int QuantityPerCombo { get; set; }
        public int QuantityChanged { get; set; }
        public int StockBefore { get; set; }
        public int StockAfter { get; set; }
    }

    public class NurseryComboStockResponseDto
    {
        public int Id { get; set; }
        public int PlantComboId { get; set; }
        public string? ComboCode { get; set; }
        public string? ComboName { get; set; }
        public int? ComboType { get; set; }
        public string? ComboTypeName { get; set; }
        public decimal? Price { get; set; }
        public int Quantity { get; set; }
        public bool IsActive { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
