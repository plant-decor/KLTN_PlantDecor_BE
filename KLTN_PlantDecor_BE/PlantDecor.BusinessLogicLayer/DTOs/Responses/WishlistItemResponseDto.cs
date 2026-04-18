using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class WishlistItemResponseDto
    {
        public int Id { get; set; }
        public WishlistItemType ItemType { get; set; }
        public int ItemId { get; set; }
        public int? PlantId { get; set; }
        public string ItemName { get; set; } = null!;
        public string? ItemImageUrl { get; set; }
        public decimal? Price { get; set; }
        public int? Quantity { get; set; }
        public string? NurseryName { get; set; }
        public string? AdditionalInfo { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
