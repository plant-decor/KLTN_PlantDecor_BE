using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class WishlistMapper
    {
        public static WishlistItemResponseDto ToResponse(this Wishlist w)
        {
            return w.ItemType switch
            {
                WishlistItemType.Plant => MapPlant(w),
                WishlistItemType.PlantInstance => MapPlantInstance(w),
                WishlistItemType.PlantCombo => MapPlantCombo(w),
                WishlistItemType.Material => MapMaterial(w),
                _ => throw new InvalidOperationException($"Unknown item type: {w.ItemType}")
            };
        }

        private static WishlistItemResponseDto MapPlant(Wishlist w) => new()
        {
            Id = w.Id,
            ItemType = w.ItemType,
            ItemId = w.PlantId ?? 0,
            ItemName = w.Plant?.Name ?? string.Empty,
            ItemImageUrl = w.Plant?.PlantImages?
                .FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                ?? w.Plant?.PlantImages?.FirstOrDefault()?.ImageUrl,
            Price = w.Plant?.BasePrice,
            AdditionalInfo = w.Plant?.Description,
            CreatedAt = w.CreatedAt
        };

        private static WishlistItemResponseDto MapPlantInstance(Wishlist w) => new()
        {
            Id = w.Id,
            ItemType = w.ItemType,
            ItemId = w.PlantInstanceId ?? 0,
            ItemName = w.PlantInstance?.Plant?.Name ?? "Plant Instance",
            ItemImageUrl = w.PlantInstance?.PlantImages?
                .FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                ?? w.PlantInstance?.PlantImages?.FirstOrDefault()?.ImageUrl
                ?? w.PlantInstance?.Plant?.PlantImages?.FirstOrDefault()?.ImageUrl,
            Price = w.PlantInstance?.SpecificPrice,
            NurseryName = w.PlantInstance?.CurrentNursery?.Name,
            AdditionalInfo = $"SKU: {w.PlantInstance?.SKU}, Height: {w.PlantInstance?.Height}cm, Status: {w.PlantInstance?.HealthStatus}",
            CreatedAt = w.CreatedAt
        };

        private static WishlistItemResponseDto MapPlantCombo(Wishlist w) => new()
        {
            Id = w.Id,
            ItemType = w.ItemType,
            ItemId = w.PlantComboId ?? 0,
            ItemName = w.PlantCombo?.ComboName ?? string.Empty,
            ItemImageUrl = w.PlantCombo?.PlantComboImages?
                .FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                ?? w.PlantCombo?.PlantComboImages?.FirstOrDefault()?.ImageUrl,
            Price = w.PlantCombo?.ComboPrice,
            AdditionalInfo = w.PlantCombo?.Description,
            CreatedAt = w.CreatedAt
        };

        private static WishlistItemResponseDto MapMaterial(Wishlist w) => new()
        {
            Id = w.Id,
            ItemType = w.ItemType,
            ItemId = w.MaterialId ?? 0,
            ItemName = w.Material?.Name ?? string.Empty,
            ItemImageUrl = w.Material?.MaterialImages?
                .FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                ?? w.Material?.MaterialImages?.FirstOrDefault()?.ImageUrl,
            Price = w.Material?.BasePrice,
            AdditionalInfo = w.Material?.Description,
            CreatedAt = w.CreatedAt
        };

        public static List<WishlistItemResponseDto> ToResponseList(this IEnumerable<Wishlist> items)
            => items.Select(w => w.ToResponse()).ToList();
    }
}
