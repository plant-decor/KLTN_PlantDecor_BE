using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class WishlistMapper
    {
        public static WishlistItemResponseDto ToResponse(this Wishlist w) => new()
        {
            PlantId = w.PlantId,
            PlantName = w.Plant?.Name ?? string.Empty,
            PlantImageUrl = w.Plant?.PlantImages?
                .FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                ?? w.Plant?.PlantImages?.FirstOrDefault()?.ImageUrl,
            CreatedAt = w.CreatedAt
        };

        public static List<WishlistItemResponseDto> ToResponseList(this IEnumerable<Wishlist> items)
            => items.Select(w => w.ToResponse()).ToList();
    }
}
