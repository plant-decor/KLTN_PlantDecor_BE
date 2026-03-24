using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class PaginationSearchRequestDto
    {
        public Pagination Pagination { get; set; } = new Pagination();
    }

    public class PlantSearchRequestDto
    {
        public Pagination Pagination { get; set; } = new Pagination();
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
        public string? SortBy { get; set; }
        public string? SortDirection { get; set; }
    }

    public class ShopPlantInstanceSearchRequestDto
    {
        public Pagination Pagination { get; set; } = new Pagination();
        public int? NurseryId { get; set; }
    }
}