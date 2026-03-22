using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CommonPlantShopSearchRequestDto
    {
        public Pagination Pagination { get; set; } = new Pagination();
        public string? SearchTerm { get; set; }
        public List<int>? CategoryIds { get; set; }
        public List<int>? TagIds { get; set; }
        public List<string>? Sizes { get; set; }
        public double? MinPrice { get; set; }
        public double? MaxPrice { get; set; }
        public string? SortBy { get; set; }
        public bool IsAscending { get; set; } = true;
    }
}
