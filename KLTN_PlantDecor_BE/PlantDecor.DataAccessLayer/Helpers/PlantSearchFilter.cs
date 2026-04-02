using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.DataAccessLayer.Helpers
{
    public class PlantSearchFilter
    {
        public string? Keyword { get; set; }
        public bool? IsActive { get; set; }
        public int? PlacementType { get; set; }
        public int? CareLevelType { get; set; }
        public string? CareLevel { get; set; }
        public bool? Toxicity { get; set; }
        public bool? AirPurifying { get; set; }
        public bool? HasFlower { get; set; }
        public bool? PetSafe { get; set; }
        public bool? ChildSafe { get; set; }
        public bool? IsUniqueInstance { get; set; }
        public decimal? MinBasePrice { get; set; }
        public decimal? MaxBasePrice { get; set; }
        public List<int>? CategoryIds { get; set; }
        public List<int>? TagIds { get; set; }
        public List<int>? Sizes { get; set; }
        public string? FengShuiElement { get; set; }
        public int? NurseryId { get; set; }
        public PlantSortByEnum? SortBy { get; set; }
        public SortDirectionEnum? SortDirection { get; set; }
    }
}
