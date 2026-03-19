namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class PlantResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? SpecificName { get; set; }
        public string? Origin { get; set; }
        public string? Description { get; set; }
        public decimal? BasePrice { get; set; }
        public int? PlacementType { get; set; }
        public string PlacementTypeName { get; set; } = null!;
        public string? Size { get; set; }
        public string? GrowthRate { get; set; }
        public bool? Toxicity { get; set; }
        public bool? AirPurifying { get; set; }
        public bool? HasFlower { get; set; }
        public bool? PetSafe { get; set; }
        public bool? ChildSafe { get; set; }
        public string? FengShuiElement { get; set; }
        public string? FengShuiMeaning { get; set; }
        public bool? PotIncluded { get; set; }
        public string? PotSize { get; set; }
        public string? CareLevel { get; set; }
        public bool? IsActive { get; set; }
        public bool? IsUniqueInstance { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Related data
        public List<CategoryResponseDto> Categories { get; set; } = new List<CategoryResponseDto>();
        public List<TagResponseDto> Tags { get; set; } = new List<TagResponseDto>();
        public List<PlantImageResponseDto> Images { get; set; } = new List<PlantImageResponseDto>();
        public int TotalInstances { get; set; }
        public int AvailableInstances { get; set; }
    }

    public class PlantImageResponseDto
    {
        public int Id { get; set; }
        public string? ImageUrl { get; set; }
        public bool? IsPrimary { get; set; }
    }

    public class PlantListResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal? BasePrice { get; set; }
        public string? Size { get; set; }
        public string? CareLevel { get; set; }
        public bool? IsActive { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public int TotalInstances { get; set; }
        public int AvailableInstances { get; set; }
        public int AvailableCommonQuantity { get; set; }
        public int TotalAvailableStock { get; set; }
        public List<string> CategoryNames { get; set; } = new List<string>();
        public List<string> TagNames { get; set; } = new List<string>();
    }
}
