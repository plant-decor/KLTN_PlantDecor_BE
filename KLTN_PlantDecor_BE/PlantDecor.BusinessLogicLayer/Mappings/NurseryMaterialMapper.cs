using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class NurseryMaterialMapper
    {
        #region Entity to Response
        public static NurseryMaterialResponseDto ToResponse(this NurseryMaterial entity)
        {
            if (entity == null) return null!;
            return new NurseryMaterialResponseDto
            {
                Id = entity.Id,
                MaterialId = entity.MaterialId,
                MaterialName = entity.Material?.Name,
                MaterialCode = entity.Material?.MaterialCode,
                Unit = entity.Material?.Unit,
                BasePrice = entity.Material?.BasePrice,
                ExpiredDate = entity.ExpiredDate,
                NurseryId = entity.NurseryId,
                NurseryName = entity.Nursery?.Name,
                Quantity = entity.Quantity,
                ReservedQuantity = entity.ReservedQuantity,
                IsActive = entity.IsActive
            };
        }

        public static NurseryMaterialListResponseDto ToListResponse(this NurseryMaterial entity)
        {
            if (entity == null) return null!;
            return new NurseryMaterialListResponseDto
            {
                Id = entity.Id,
                MaterialId = entity.MaterialId,
                MaterialName = entity.Material?.Name,
                MaterialCode = entity.Material?.MaterialCode,
                Unit = entity.Material?.Unit,
                BasePrice = entity.Material?.BasePrice,
                ExpiredDate = entity.ExpiredDate,
                NurseryId = entity.NurseryId,
                NurseryName = entity.Nursery?.Name,
                Quantity = entity.Quantity,
                ReservedQuantity = entity.ReservedQuantity,
                IsActive = entity.IsActive
            };
        }

        public static List<NurseryMaterialResponseDto> ToResponseList(this IEnumerable<NurseryMaterial> entities)
        {
            return entities.Select(e => e.ToResponse()).ToList();
        }

        public static List<NurseryMaterialListResponseDto> ToListResponseList(this IEnumerable<NurseryMaterial> entities)
        {
            return entities.Select(e => e.ToListResponse()).ToList();
        }
        #endregion

        #region Request to Entity
        public static NurseryMaterial ToEntity(this NurseryMaterialRequestDto request)
        {
            if (request == null) return null!;

            return new NurseryMaterial
            {
                MaterialId = request.MaterialId,
                NurseryId = request.NurseryId,
                Quantity = request.Quantity,
                ExpiredDate = request.ExpiredDate,
                ReservedQuantity = 0,
                IsActive = request.IsActive
            };
        }
        #endregion

        #region Update Entity
        public static void ToUpdate(this NurseryMaterialUpdateDto request, NurseryMaterial entity)
        {
            if (request == null || entity == null) return;

            if (request.Quantity.HasValue) entity.Quantity = request.Quantity.Value;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive.Value;
            if (request.ExpiredDate.HasValue) entity.ExpiredDate = request.ExpiredDate.Value;
        }
        #endregion
    }
}
