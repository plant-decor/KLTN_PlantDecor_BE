using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class NurseryMapper
    {
        private static readonly Dictionary<int, string> NurseryTypes = new()
        {
            { 1, "Indoor" },
            { 2, "Outdoor" },
            { 3, "Mixed" }
        };

        private static readonly Dictionary<int, string> LightConditions = new()
        {
            { 1, "Low" },
            { 2, "Medium" },
            { 3, "High" }
        };

        private static readonly Dictionary<int, string> HumidityLevels = new()
        {
            { 1, "Low" },
            { 2, "Medium" },
            { 3, "High" }
        };

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
                Type = entity.Type,
                TypeName = entity.Type.HasValue && NurseryTypes.ContainsKey(entity.Type.Value) 
                    ? NurseryTypes[entity.Type.Value] : null,
                LightCondition = entity.LightCondition,
                LightConditionName = entity.LightCondition.HasValue && LightConditions.ContainsKey(entity.LightCondition.Value)
                    ? LightConditions[entity.LightCondition.Value] : null,
                HumidityLevel = entity.HumidityLevel,
                HumidityLevelName = entity.HumidityLevel.HasValue && HumidityLevels.ContainsKey(entity.HumidityLevel.Value)
                    ? HumidityLevels[entity.HumidityLevel.Value] : null,
                HasMistSystem = entity.HasMistSystem,
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
                Type = entity.Type,
                TypeName = entity.Type.HasValue && NurseryTypes.ContainsKey(entity.Type.Value)
                    ? NurseryTypes[entity.Type.Value] : null,
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
        public static Nursery ToEntity(this NurseryRequestDto request, int managerId)
        {
            if (request == null) return null!;

            return new Nursery
            {
                ManagerId = managerId,
                Name = request.Name,
                Address = request.Address,
                Area = request.Area,
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                Phone = request.Phone,
                Type = request.Type,
                LightCondition = request.LightCondition,
                HumidityLevel = request.HumidityLevel,
                HasMistSystem = request.HasMistSystem,
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
            if (request.Type.HasValue) entity.Type = request.Type;
            if (request.LightCondition.HasValue) entity.LightCondition = request.LightCondition;
            if (request.HumidityLevel.HasValue) entity.HumidityLevel = request.HumidityLevel;
            if (request.HasMistSystem.HasValue) entity.HasMistSystem = request.HasMistSystem;
            if (request.IsActive.HasValue) entity.IsActive = request.IsActive;
        }
        #endregion
    }
}
