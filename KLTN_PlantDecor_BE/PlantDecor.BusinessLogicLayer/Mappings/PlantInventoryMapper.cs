using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class PlantInventoryMapper
    {
        #region Entity to Response
        public static PlantInventoryResponseDto ToResponse(this PlantInventory entity)
        {
            if (entity == null) return null!;
            return new PlantInventoryResponseDto
            {
                Id = entity.Id,
                PlantId = entity.PlantId,
                PlantName = entity.Plant?.Name,
                NurseryId = entity.NurseryId,
                NurseryName = entity.Nursery?.Name,
                Quantity = entity.Quantity,
                ReservedQuantity = entity.ReservedQuantity
            };
        }

        public static PlantInventoryListResponseDto ToListResponse(this PlantInventory entity)
        {
            if (entity == null) return null!;
            return new PlantInventoryListResponseDto
            {
                Id = entity.Id,
                PlantId = entity.PlantId,
                PlantName = entity.Plant?.Name,
                NurseryId = entity.NurseryId,
                NurseryName = entity.Nursery?.Name,
                Quantity = entity.Quantity,
                ReservedQuantity = entity.ReservedQuantity
            };
        }

        public static List<PlantInventoryResponseDto> ToResponseList(this IEnumerable<PlantInventory> entities)
        {
            return entities.Select(e => e.ToResponse()).ToList();
        }

        public static List<PlantInventoryListResponseDto> ToListResponseList(this IEnumerable<PlantInventory> entities)
        {
            return entities.Select(e => e.ToListResponse()).ToList();
        }
        #endregion

        #region Request to Entity
        public static PlantInventory ToEntity(this PlantInventoryRequestDto request)
        {
            if (request == null) return null!;

            return new PlantInventory
            {
                PlantId = request.PlantId,
                NurseryId = request.NurseryId,
                Quantity = request.Quantity,
                ReservedQuantity = 0
            };
        }
        #endregion

        #region Update Entity
        public static void ToUpdate(this PlantInventoryUpdateDto request, PlantInventory entity)
        {
            if (request == null || entity == null) return;

            entity.Quantity = request.Quantity ?? entity.Quantity;
            entity.ReservedQuantity = request.ReservedQuantity ?? entity.ReservedQuantity;
        }
        #endregion
    }
}
