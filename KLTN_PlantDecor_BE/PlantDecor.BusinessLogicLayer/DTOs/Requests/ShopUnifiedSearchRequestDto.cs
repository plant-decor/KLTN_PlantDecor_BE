using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class ShopUnifiedSearchRequestDto
    {
        public Pagination Pagination { get; set; } = new Pagination();
        public string? Keyword { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public List<int>? CategoryIds { get; set; }
        public List<int>? TagIds { get; set; }
        public bool? PetSafe { get; set; }
        public bool? ChildSafe { get; set; }
        public int? PlacementType { get; set; }
        public int? CareLevelType { get; set; }
        public string? CareLevel { get; set; }
        public bool? Toxicity { get; set; }
        public bool? AirPurifying { get; set; }
        public bool? HasFlower { get; set; }
        public bool? IsUniqueInstance { get; set; }
        public List<int>? Sizes { get; set; }
        public string? FengShuiElement { get; set; }
        public int? NurseryId { get; set; }
        public UnifiedSearchSortByEnum? SortBy { get; set; }
        public SortDirectionEnum? SortDirection { get; set; }
        public bool IncludePlants { get; set; } = true;
        public bool IncludeMaterials { get; set; } = true;
        public bool IncludeCombos { get; set; } = true;
    }
}