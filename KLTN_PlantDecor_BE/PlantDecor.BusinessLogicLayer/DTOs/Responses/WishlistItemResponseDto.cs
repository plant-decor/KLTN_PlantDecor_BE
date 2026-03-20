namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class WishlistItemResponseDto
    {
        public int PlantId { get; set; }
        public string PlantName { get; set; } = null!;
        public string? PlantImageUrl { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
