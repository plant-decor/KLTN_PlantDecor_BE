using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class ShopUnifiedSearchResponseDto
    {
        public string? Keyword { get; set; }
        public PaginatedResult<PlantListResponseDto> Plants { get; set; } = new();
        public PaginatedResult<NurseryMaterialListResponseDto> Materials { get; set; } = new();
        public PaginatedResult<SellingPlantComboResponseDto> Combos { get; set; } = new();
    }
}