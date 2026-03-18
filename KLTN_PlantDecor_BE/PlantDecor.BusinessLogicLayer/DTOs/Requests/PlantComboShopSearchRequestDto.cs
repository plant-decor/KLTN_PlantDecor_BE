using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class PlantComboShopSearchRequestDto
    {
        public Pagination Pagination { get; set; } = new Pagination();
        public string? Keyword { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public int? CategoryId { get; set; }
        public List<int>? TagIds { get; set; }
    }
}
