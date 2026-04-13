using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class PlantGuideMapper
    {
        #region Entity to Response
        public static PlantGuideResponseDto ToResponse(this PlantGuide entity)
        {
            if (entity == null) return null!;

            return new PlantGuideResponseDto
            {
                Id = entity.Id,
                PlantId = entity.PlantId,
                PlantName = entity.Plant?.Name,
                LightRequirement = entity.LightRequirement,
                LightRequirementName = GetLightRequirementName(entity.LightRequirement),
                Watering = entity.Watering,
                Fertilizing = entity.Fertilizing,
                Pruning = entity.Pruning,
                Temperature = entity.Temperature,
                Humidity = entity.Humidity,
                Soil = entity.Soil,
                CareNotes = entity.CareNotes,
                CreatedAt = entity.CreatedAt
            };
        }

        public static List<PlantGuideResponseDto> ToResponseList(this IEnumerable<PlantGuide> entities)
        {
            return entities.Select(e => e.ToResponse()).ToList();
        }
        #endregion

        #region Request to Entity
        public static PlantGuide ToEntity(this PlantGuideRequestDto request)
        {
            if (request == null) return null!;

            return new PlantGuide
            {
                PlantId = request.PlantId,
                LightRequirement = (int?)request.LightRequirement,
                Watering = request.Watering,
                Fertilizing = request.Fertilizing,
                Pruning = request.Pruning,
                Temperature = request.Temperature,
                Humidity = request.Humidity,
                Soil = request.Soil,
                CareNotes = request.CareNotes,
                CreatedAt = DateTime.Now
            };
        }
        #endregion

        #region Update Entity
        public static void ToUpdate(this PlantGuideUpdateDto request, PlantGuide entity)
        {
            if (request == null || entity == null) return;

            if (request.PlantId.HasValue)
                entity.PlantId = request.PlantId.Value;

            if (request.LightRequirement.HasValue)
                entity.LightRequirement = (int)request.LightRequirement.Value;

            if (request.Watering != null)
                entity.Watering = request.Watering;

            if (request.Fertilizing != null)
                entity.Fertilizing = request.Fertilizing;

            if (request.Pruning != null)
                entity.Pruning = request.Pruning;

            if (request.Temperature != null)
                entity.Temperature = request.Temperature;

            if (request.Humidity != null)
                entity.Humidity = request.Humidity;

            if (request.Soil != null)
                entity.Soil = request.Soil;

            if (request.CareNotes != null)
                entity.CareNotes = request.CareNotes;
        }
        #endregion

        private static string? GetLightRequirementName(int? lightRequirement)
        {
            if (!lightRequirement.HasValue)
            {
                return null;
            }

            if (!Enum.IsDefined(typeof(LightRequirementEnum), lightRequirement.Value))
            {
                return null;
            }

            return ((LightRequirementEnum)lightRequirement.Value).ToString();
        }
    }
}
