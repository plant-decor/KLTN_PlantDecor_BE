using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class PlantInstanceMapper
    {
        #region Entity to Response
        public static PlantInstanceResponseDto ToResponse(this PlantInstance instance)
        {
            if (instance == null) return null!;
            return new PlantInstanceResponseDto
            {
                Id = instance.Id,
                PlantId = instance.PlantId,
                PlantName = instance.Plant?.Name,
                SpecificPrice = instance.SpecificPrice,
                Height = instance.Height,
                TrunkDiameter = instance.TrunkDiameter,
                HealthStatus = instance.HealthStatus,
                Age = instance.Age,
                Description = instance.Description,
                Status = instance.Status,
                CreatedAt = instance.CreatedAt,
                UpdatedAt = instance.UpdatedAt
            };
        }

        public static List<PlantInstanceResponseDto> ToResponseList(this IEnumerable<PlantInstance> instances)
        {
            return instances.Select(i => i.ToResponse()).ToList();
        }
        #endregion

        #region Request to Entity
        public static PlantInstance ToEntity(this PlantInstanceRequestDto request, decimal? basePrice = null)
        {
            if (request == null) return null!;

            return new PlantInstance
            {
                PlantId = request.PlantId,
                SpecificPrice = request.SpecificPrice ?? basePrice,
                Height = request.Height,
                TrunkDiameter = request.TrunkDiameter,
                HealthStatus = request.HealthStatus ?? "Good",
                Age = request.Age,
                Description = request.Description,
                Status = request.Status ?? "Available",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }
        #endregion

        #region Update Entity
        public static void ToUpdate(this PlantInstanceUpdateDto request, PlantInstance instance)
        {
            if (request == null || instance == null) return;

            instance.SpecificPrice = request.SpecificPrice ?? instance.SpecificPrice;
            instance.Height = request.Height ?? instance.Height;
            instance.TrunkDiameter = request.TrunkDiameter ?? instance.TrunkDiameter;
            instance.HealthStatus = request.HealthStatus ?? instance.HealthStatus;
            instance.Age = request.Age ?? instance.Age;
            instance.Description = request.Description ?? instance.Description;
            instance.Status = request.Status ?? instance.Status;
            instance.UpdatedAt = DateTime.Now;
        }
        #endregion
    }
}
