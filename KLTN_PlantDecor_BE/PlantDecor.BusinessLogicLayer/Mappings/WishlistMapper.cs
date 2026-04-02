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
                WishlistItemType.CommonPlant => MapCommonPlant(w),
                WishlistItemType.PlantInstance => MapPlantInstance(w),
                WishlistItemType.NurseryPlantCombo => MapNurseryPlantCombo(w),
                WishlistItemType.NurseryMaterial => MapNurseryMaterial(w),
                _ => throw new InvalidOperationException($"Unknown item type: {w.ItemType}")
            };
        }

        private static WishlistItemResponseDto MapCommonPlant(Wishlist w) => new()
        {
            Id = w.Id,
            ItemType = w.ItemType,
            ItemId = w.CommonPlantId ?? 0,
            ItemName = w.CommonPlant?.Plant?.Name ?? string.Empty,
            ItemImageUrl = w.CommonPlant?.Plant?.PlantImages?
                .FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                ?? w.CommonPlant?.Plant?.PlantImages?.FirstOrDefault()?.ImageUrl,
            Price = w.CommonPlant?.Plant?.BasePrice,
            Quantity = w.CommonPlant?.Quantity,
            AdditionalInfo = $"Nursery: {w.CommonPlant?.Nursery?.Name}, Available: {w.CommonPlant?.Quantity}",
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
            AdditionalInfo = $"SKU: {w.PlantInstance?.SKU}, Height: {w.PlantInstance?.Height}cm, Status: {w.PlantInstance?.HealthStatus}",
            CreatedAt = w.CreatedAt
        };

        private static WishlistItemResponseDto MapNurseryPlantCombo(Wishlist w) => new()
        {
            Id = w.Id,
            ItemType = w.ItemType,
            ItemId = w.NurseryPlantComboId ?? 0,
            ItemName = w.NurseryPlantCombo?.PlantCombo?.ComboName ?? string.Empty,
            ItemImageUrl = w.NurseryPlantCombo?.PlantCombo?.PlantComboImages?
                .FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                ?? w.NurseryPlantCombo?.PlantCombo?.PlantComboImages?.FirstOrDefault()?.ImageUrl,
            Price = w.NurseryPlantCombo?.PlantCombo?.ComboPrice,
            Quantity = w.NurseryPlantCombo?.Quantity,
            AdditionalInfo = w.NurseryPlantCombo?.PlantCombo?.Description,
            CreatedAt = w.CreatedAt
        };

        private static WishlistItemResponseDto MapNurseryMaterial(Wishlist w) => new()
        {
            Id = w.Id,
            ItemType = w.ItemType,
            ItemId = w.NurseryMaterialId ?? 0,
            ItemName = w.NurseryMaterial?.Material?.Name ?? string.Empty,
            ItemImageUrl = w.NurseryMaterial?.Material?.MaterialImages?
                .FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                ?? w.NurseryMaterial?.Material?.MaterialImages?.FirstOrDefault()?.ImageUrl,
            Price = w.NurseryMaterial?.Material?.BasePrice,
            Quantity = w.NurseryMaterial?.Quantity,
            AdditionalInfo = $"Available: {w.NurseryMaterial?.Quantity}, Expired: {w.NurseryMaterial?.ExpiredDate}",
            CreatedAt = w.CreatedAt
        };

        public static List<WishlistItemResponseDto> ToResponseList(this IEnumerable<Wishlist> items)
            => items.Select(w => w.ToResponse()).ToList();
    }
}
