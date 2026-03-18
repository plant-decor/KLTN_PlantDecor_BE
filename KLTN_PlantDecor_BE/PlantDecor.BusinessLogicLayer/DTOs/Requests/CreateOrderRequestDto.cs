namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CreateOrderRequestDto
    {
        public int NurseryId { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? CustomerName { get; set; }
        public string? Note { get; set; }
        public int PaymentStrategy { get; set; }
        public int OrderType { get; set; }
        public List<CreateOrderItemDto> Items { get; set; } = new();
    }

    public class CreateOrderItemDto
    {
        public int? CommonPlantId { get; set; }
        public int? PlantInstanceId { get; set; }
        public int? NurseryPlantComboId { get; set; }
        public int? NurseryMaterialId { get; set; }
        public string? ItemName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
