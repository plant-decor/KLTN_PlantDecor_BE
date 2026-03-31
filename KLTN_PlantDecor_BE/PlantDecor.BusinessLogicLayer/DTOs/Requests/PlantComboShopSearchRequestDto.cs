using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class PlantComboShopSearchRequestDto
    {
        public Pagination Pagination { get; set; } = new Pagination();
        public string? Keyword { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool? PetSafe { get; set; }
        public bool? ChildSafe { get; set; }
        public List<int>? CategoryIds { get; set; }
        public int? CategoryId { get; set; }
        public List<int>? TagIds { get; set; }
        public PlantComboSortByEnum? SortBy { get; set; }
        public SortDirectionEnum? SortDirection { get; set; }
    }
}
