namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class CartItemResponseDto
    {
        public int Id { get; set; }
        public int? CartId { get; set; }
        public int? CommonPlantId { get; set; }
        public int? NurseryPlantComboId { get; set; }
        public int? NurseryMaterialId { get; set; }
        public string? ProductName { get; set; }
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
        public decimal? SubTotal => (Quantity ?? 0) * (Price ?? 0);
        public DateTime? CreatedAt { get; set; }
    }

    public class CartResponseDto
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<CartItemResponseDto> CartItems { get; set; } = [];
        public decimal TotalPrice => CartItems.Sum(i => i.SubTotal ?? 0);
        public int TotalItems => CartItems.Sum(i => i.Quantity ?? 0);
    }
}
