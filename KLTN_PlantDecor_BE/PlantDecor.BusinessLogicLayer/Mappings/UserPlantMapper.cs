using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class UserPlantMapper
    {
        public static UserPlantResponseDto ToResponse(this UserPlant userPlant)
        {
            var resolvedPlant = userPlant.PlantInstance?.Plant ?? userPlant.Plant;
            var imageUrl = SelectPrimaryImageUrl(userPlant);

            return new UserPlantResponseDto
            {
                Id = userPlant.Id,
                PlantId = userPlant.PlantId,
                PlantInstanceId = userPlant.PlantInstanceId,
                PlantName = resolvedPlant?.Name,
                PlantSpecificName = resolvedPlant?.SpecificName,
                PrimaryImageUrl = imageUrl,
                PurchaseDate = userPlant.PurchaseDate,
                LastWateredDate = userPlant.LastWateredDate,
                LastFertilizedDate = userPlant.LastFertilizedDate,
                LastPrunedDate = userPlant.LastPrunedDate,
                Location = userPlant.Location,
                CurrentTrunkDiameter = userPlant.CurrentTrunkDiameter,
                CurrentHeight = userPlant.CurrentHeight,
                HealthStatus = userPlant.HealthStatus,
                Age = userPlant.Age,
                CreatedAt = userPlant.CreatedAt,
                UpdatedAt = userPlant.UpdatedAt
            };
        }

        public static List<UserPlantResponseDto> ToResponseList(this IEnumerable<UserPlant> userPlants)
        {
            return userPlants.Select(userPlant => userPlant.ToResponse()).ToList();
        }

        private static string? SelectPrimaryImageUrl(UserPlant userPlant)
        {
            var plantInstanceImage = userPlant.PlantInstance?.PlantImages
                .Where(image => !string.IsNullOrWhiteSpace(image.ImageUrl))
                .OrderByDescending(image => image.IsPrimary == true)
                .ThenByDescending(image => image.Id)
                .Select(image => image.ImageUrl)
                .FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(plantInstanceImage))
            {
                return plantInstanceImage;
            }

            return userPlant.Plant?.PlantImages
                .Where(image => !string.IsNullOrWhiteSpace(image.ImageUrl))
                .OrderByDescending(image => image.IsPrimary == true)
                .ThenByDescending(image => image.Id)
                .Select(image => image.ImageUrl)
                .FirstOrDefault();
        }
    }
}