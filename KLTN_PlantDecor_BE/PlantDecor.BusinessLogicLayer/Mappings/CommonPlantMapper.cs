using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class CommonPlantMapper
    {
        #region Entity to Response
        public static CommonPlantResponseDto ToResponse(this CommonPlant entity)
        {
            if (entity == null) return null!;
            return new CommonPlantResponseDto
            {
                Id = entity.Id,
                PlantId = entity.PlantId,
                PlantName = entity.Plant?.Name,
                NurseryId = entity.NurseryId,
                NurseryName = entity.Nursery?.Name,
                Quantity = entity.Quantity,
                ReservedQuantity = entity.ReservedQuantity,
                IsActive = entity.IsActive
            };
        }

        public static CommonPlantListResponseDto ToListResponse(this CommonPlant entity)
        {
            if (entity == null) return null!;
            return new CommonPlantListResponseDto
            {
                Id = entity.Id,
                PlantId = entity.PlantId,
                PlantName = entity.Plant?.Name,
                NurseryId = entity.NurseryId,
                NurseryName = entity.Nursery?.Name,
                Quantity = entity.Quantity,
                ReservedQuantity = entity.ReservedQuantity,
                IsActive = entity.IsActive
            };
        }

        public static List<CommonPlantResponseDto> ToResponseList(this IEnumerable<CommonPlant> entities)
        {
            return entities.Select(e => e.ToResponse()).ToList();
        }

        public static List<CommonPlantListResponseDto> ToListResponseList(this IEnumerable<CommonPlant> entities)
        {
            return entities.Select(e => e.ToListResponse()).ToList();
        }
        #endregion

        #region Request to Entity
        public static CommonPlant ToEntity(this CommonPlantRequestDto request)
        {
            if (request == null) return null!;

            return new CommonPlant
            {
                PlantId = request.PlantId,
                Quantity = request.Quantity,
                ReservedQuantity = 0,
                IsActive = request.IsActive
            };
        }
        #endregion

        #region Update Entity
        public static void ToUpdate(this CommonPlantUpdateDto request, CommonPlant entity)
        {
            if (request == null || entity == null) return;

            entity.Quantity = request.Quantity ?? entity.Quantity;
            entity.ReservedQuantity = request.ReservedQuantity ?? entity.ReservedQuantity;
            entity.IsActive = request.IsActive ?? entity.IsActive;
        }
        #endregion
    }
}
