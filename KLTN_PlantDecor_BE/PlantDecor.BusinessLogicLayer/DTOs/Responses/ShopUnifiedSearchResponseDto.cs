using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class ShopUnifiedSearchResponseDto
    {
        public string? Keyword { get; set; }
        public PaginatedResult<ShopSearchItemDto> Items { get; set; } = new();
        public int PlantTotalCount { get; set; }
        public int MaterialTotalCount { get; set; }
        public int ComboTotalCount { get; set; }
    }
}