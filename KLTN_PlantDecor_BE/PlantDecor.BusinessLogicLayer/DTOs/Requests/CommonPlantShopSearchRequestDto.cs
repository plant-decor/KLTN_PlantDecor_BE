using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CommonPlantShopSearchRequestDto
    {
        public Pagination Pagination { get; set; } = new Pagination();
        public string? SearchTerm { get; set; }
        public List<int>? CategoryIds { get; set; }
        public List<int>? TagIds { get; set; }
        public List<int>? Sizes { get; set; }
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
        public CommonPlantSortByEnum? SortBy { get; set; }
        public SortDirectionEnum? SortDirection { get; set; }
    }
}
