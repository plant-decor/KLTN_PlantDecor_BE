using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class CartMapper
    {
        public static CartItemResponseDto ToResponse(this CartItem item) => new()
        {
            Id = item.Id,
            CartId = item.CartId,
            CommonPlantId = item.CommonPlantId,
            NurseryPlantComboId = item.NurseryPlantComboId,
            NurseryMaterialId = item.NurseryMaterialId,
            PlantId = item.CommonPlant?.PlantId,
            PlantComboId = item.NurseryPlantCombo?.PlantComboId,
            MaterialId = item.NurseryMaterial?.MaterialId,
            NurseryId = item.CommonPlant?.NurseryId
                ?? item.NurseryPlantCombo?.NurseryId
                ?? item.NurseryMaterial?.NurseryId
                ?? 0,
            NurseryName = item.CommonPlant?.Nursery?.Name
                ?? item.NurseryPlantCombo?.Nursery?.Name
                ?? item.NurseryMaterial?.Nursery?.Name
                ?? string.Empty,
            ProductName = item.CommonPlant?.Plant?.Name
                ?? item.NurseryPlantCombo?.PlantCombo?.ComboName
                ?? item.NurseryMaterial?.Material?.Name,
            PrimaryImageUrl = item.CommonPlant?.Plant?.PlantImages.FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                ?? item.CommonPlant?.Plant?.PlantImages.FirstOrDefault()?.ImageUrl
                ?? item.NurseryPlantCombo?.PlantCombo?.PlantComboImages.FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                ?? item.NurseryPlantCombo?.PlantCombo?.PlantComboImages.FirstOrDefault()?.ImageUrl
                ?? item.NurseryMaterial?.Material?.MaterialImages.FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                ?? item.NurseryMaterial?.Material?.MaterialImages.FirstOrDefault()?.ImageUrl,
            Quantity = item.Quantity,
            Price = item.Price,
            CreatedAt = item.CreatedAt
        };

        public static List<CartItemResponseDto> ToResponseList(this IEnumerable<CartItem> items)
            => items.Select(i => i.ToResponse()).ToList();
    }
}
