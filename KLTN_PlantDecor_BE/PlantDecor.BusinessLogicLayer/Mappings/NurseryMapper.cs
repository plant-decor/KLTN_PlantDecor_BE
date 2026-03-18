using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class NurseryMapper
    {
        #region Entity to Response
        public static NurseryResponseDto ToResponse(this Nursery entity)
        {
            if (entity == null) return null!;
            return new NurseryResponseDto
            {
                Id = entity.Id,
                ManagerId = entity.ManagerId,
                ManagerName = entity.Manager?.Username,
                Name = entity.Name,
                Address = entity.Address,
                Area = entity.Area,
                Latitude = entity.Latitude,
                Longitude = entity.Longitude,
                Phone = entity.Phone,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt,
                TotalPlants = entity.CommonPlants?.Count ?? 0,
                TotalMaterials = entity.NurseryMaterials?.Count ?? 0
            };
        }

        public static NurseryListResponseDto ToListResponse(this Nursery entity)
        {
            if (entity == null) return null!;
            return new NurseryListResponseDto
            {
                Id = entity.Id,
                ManagerId = entity.ManagerId,
                ManagerName = entity.Manager?.Username,
                Name = entity.Name,
                Address = entity.Address,
                Phone = entity.Phone,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt
            };
        }

        public static List<NurseryResponseDto> ToResponseList(this IEnumerable<Nursery> entities)
        {
            return entities.Select(e => e.ToResponse()).ToList();
        }

        public static List<NurseryListResponseDto> ToListResponseList(this IEnumerable<Nursery> entities)
        {
            return entities.Select(e => e.ToListResponse()).ToList();
        }
        #endregion

        #region Request to Entity
        public static Nursery ToEntity(this NurseryRequestDto request)
        {
            if (request == null) return null!;

            return new Nursery
            {
                ManagerId = null,
                Name = request.Name,
                Address = request.Address,
                Area = request.Area,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Phone = request.Phone,
                IsActive = request.IsActive,
                CreatedAt = DateTime.UtcNow
            };
        }
        #endregion

        #region Update Entity
        public static void ToUpdate(this NurseryUpdateDto request, Nursery entity)
        {
            if (request == null || entity == null) return;

            if (request.Name != null) entity.Name = request.Name;
            if (request.Address != null) entity.Address = request.Address;
            if (request.Area.HasValue) entity.Area = request.Area;
            if (request.Latitude.HasValue) entity.Latitude = request.Latitude;
            if (request.Longitude.HasValue) entity.Longitude = request.Longitude;
            if (request.Phone != null) entity.Phone = request.Phone;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive;
        }
        #endregion
    }
}
