namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class ShopSearchItemDto
    {
        public string Type { get; set; } = string.Empty;
        public PlantListResponseDto? Plant { get; set; }
        public NurseryMaterialListResponseDto? Material { get; set; }
        public SellingPlantComboResponseDto? Combo { get; set; }
    }
}
