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
            NurseryId = item.CommonPlant?.NurseryId
                ?? item.NurseryPlantCombo?.NurseryId
                ?? item.NurseryMaterial?.NurseryId
                ?? 0,
            ProductName = item.CommonPlant?.Plant?.Name
                ?? item.NurseryPlantCombo?.PlantCombo?.ComboName
                ?? item.NurseryMaterial?.Material?.Name,
            Quantity = item.Quantity,
            Price = item.Price,
            CreatedAt = item.CreatedAt
        };

        public static List<CartItemResponseDto> ToResponseList(this IEnumerable<CartItem> items)
            => items.Select(i => i.ToResponse()).ToList();
    }
}
