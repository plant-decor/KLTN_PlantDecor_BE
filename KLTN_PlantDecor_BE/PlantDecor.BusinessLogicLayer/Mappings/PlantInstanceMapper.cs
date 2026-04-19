using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class PlantInstanceMapper
    {
        private static readonly Dictionary<int, string> StatusNames = new()
        {
            { (int)PlantInstanceStatusEnum.Available, "Available" },
            { (int)PlantInstanceStatusEnum.Sold, "Sold" },
            { (int)PlantInstanceStatusEnum.Reserved, "Reserved" },
            { (int)PlantInstanceStatusEnum.Damaged, "Damaged" },
            { (int)PlantInstanceStatusEnum.Inactive, "Inactive" }
        };

        public static string GetStatusName(int status)
        {
            return StatusNames.TryGetValue(status, out var name) ? name : "Unknown";
        }

        #region Entity to Response

        public static PlantInstanceResponseDto ToResponse(this PlantInstance entity)
        {
            if (entity == null) return null!;
            return new PlantInstanceResponseDto
            {
                Id = entity.Id,
                PlantId = entity.PlantId,
                PlantName = entity.Plant?.Name,
                CurrentNurseryId = entity.CurrentNurseryId,
                NurseryName = entity.CurrentNursery?.Name,
                NurseryAddress = entity.CurrentNursery?.Address,
                NurseryPhone = entity.CurrentNursery?.Phone,
                SKU = entity.SKU,
                SpecificPrice = entity.SpecificPrice,
                Height = entity.Height,
                TrunkDiameter = entity.TrunkDiameter,
                HealthStatus = entity.HealthStatus,
                Age = entity.Age,
                Description = entity.Description,
                Status = entity.Status,
                StatusName = GetStatusName(entity.Status),
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt,
                Images = entity.PlantImages?.Select(i => new PlantInstanceImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsPrimary = i.IsPrimary
                }).ToList() ?? new()
            };
        }

        public static PlantInstanceListResponseDto ToListResponse(this PlantInstance entity)
        {
            if (entity == null) return null!;
            return new PlantInstanceListResponseDto
            {
                PlantInstanceId = entity.Id,
                PlantId = entity.PlantId,
                PlantName = entity.Plant?.Name,
                CurrentNurseryId = entity.CurrentNurseryId,
                NurseryName = entity.CurrentNursery?.Name,
                NurseryAddress = entity.CurrentNursery?.Address,
                NurseryPhone = entity.CurrentNursery?.Phone,
                SKU = entity.SKU,
                SpecificPrice = entity.SpecificPrice,
                Height = entity.Height,
                HealthStatus = entity.HealthStatus,
                Description = entity.Description,
                Status = entity.Status,
                StatusName = GetStatusName(entity.Status),
                PrimaryImageUrl = entity.PlantImages?.FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl,
                CreatedAt = entity.CreatedAt
            };
        }

        public static List<PlantInstanceResponseDto> ToResponseList(this IEnumerable<PlantInstance> entities)
        {
            return entities.Select(e => e.ToResponse()).ToList();
        }

        public static List<PlantInstanceListResponseDto> ToListResponseList(this IEnumerable<PlantInstance> entities)
        {
            return entities.Select(e => e.ToListResponse()).ToList();
        }

        #endregion

        #region Request to Entity

        public static PlantInstance ToEntity(this PlantInstanceItemDto request, int nurseryId)
        {
            if (request == null) return null!;
            return new PlantInstance
            {
                PlantId = request.PlantId,
                CurrentNurseryId = nurseryId,
                SKU = request.SKU,
                SpecificPrice = request.SpecificPrice,
                Height = request.Height,
                TrunkDiameter = request.TrunkDiameter,
                HealthStatus = request.HealthStatus,
                Age = request.Age,
                Description = request.Description,
                Status = (int)PlantInstanceStatusEnum.Available,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        public static List<PlantInstance> ToEntityList(this IEnumerable<PlantInstanceItemDto> requests, int nurseryId)
        {
            return requests.Select(r => r.ToEntity(nurseryId)).ToList();
        }

        #endregion
    }
}
