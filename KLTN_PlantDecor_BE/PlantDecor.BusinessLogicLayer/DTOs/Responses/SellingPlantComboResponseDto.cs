namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class SellingNurseryResponseDto
    {
        public int NurseryId { get; set; }
        public string NurseryName { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    public class SellingPlantComboResponseDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string? ImageUrl { get; set; }
        public List<SellingNurseryResponseDto> Nurseries { get; set; } = new();
    }
}
