using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class PlantMapper
    {
        #region Entity to Response
        public static PlantResponseDto ToResponse(this Plant plant)
        {
            if (plant == null) return null!;
            return new PlantResponseDto
            {
                Id = plant.Id,
                Name = plant.Name,
                SpecificName = plant.SpecificName,
                Origin = plant.Origin,
                Description = plant.Description,
                BasePrice = plant.BasePrice,
                Placement = plant.Placement,
                Size = plant.Size,
                MinHeight = plant.MinHeight,
                MaxHeight = plant.MaxHeight,
                GrowthRate = plant.GrowthRate,
                Toxicity = plant.Toxicity,
                AirPurifying = plant.AirPurifying,
                HasFlower = plant.HasFlower,
                FengShuiElement = plant.FengShuiElement,
                FengShuiMeaning = plant.FengShuiMeaning,
                PotIncluded = plant.PotIncluded,
                PotSize = plant.PotSize,
                PlantType = plant.PlantType,
                CareLevel = plant.CareLevel,
                IsActive = plant.IsActive,
                CreatedAt = plant.CreatedAt,
                UpdatedAt = plant.UpdatedAt,
                Categories = plant.Categories.Select(c => new CategoryResponseDto
                {
                    Id = c.Id,
                    Name = c.Name,
                    ParentCategoryId = c.ParentCategoryId,
                    IsActive = c.IsActive
                }).ToList(),
                Tags = plant.Tags.Select(t => new TagResponseDto
                {
                    Id = t.Id,
                    TagName = t.TagName
                }).ToList(),
                Images = plant.PlantImages.Select(i => new PlantImageResponseDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsPrimary = i.IsPrimary
                }).ToList(),
                Instances = plant.PlantInstances.Select(i => new PlantInstanceResponseDto
                {
                    Id = i.Id,
                    PlantId = i.PlantId,
                    SpecificPrice = i.SpecificPrice,
                    Height = i.Height,
                    TrunkDiameter = i.TrunkDiameter,
                    HealthStatus = i.HealthStatus,
                    Age = i.Age,
                    Description = i.Description,
                    Status = i.Status,
                    CreatedAt = i.CreatedAt,
                    UpdatedAt = i.UpdatedAt
                }).ToList(),
                TotalInstances = plant.PlantInstances.Count,
                AvailableInstances = plant.PlantInstances.Count(i => i.Status == (int)PlantInstanceStatusEnum.Available)
            };
        }

        public static PlantListResponseDto ToListResponse(this Plant plant)
        {
            if (plant == null) return null!;
            return new PlantListResponseDto
            {
                Id = plant.Id,
                Name = plant.Name,
                BasePrice = plant.BasePrice,
                Size = plant.Size,
                CareLevel = plant.CareLevel,
                IsActive = plant.IsActive,
                PrimaryImageUrl = plant.PlantImages.FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                    ?? plant.PlantImages.FirstOrDefault()?.ImageUrl,
                TotalInstances = plant.PlantInstances.Count,
                AvailableInstances = plant.PlantInstances.Count(i => i.Status == (int)PlantInstanceStatusEnum.Available),
                CategoryNames = plant.Categories.Select(c => c.Name).ToList(),
                TagNames = plant.Tags.Select(t => t.TagName).ToList()
            };
        }

        public static List<PlantResponseDto> ToResponseList(this IEnumerable<Plant> plants)
        {
            return plants.Select(p => p.ToResponse()).ToList();
        }

        public static List<PlantListResponseDto> ToListResponseList(this IEnumerable<Plant> plants)
        {
            return plants.Select(p => p.ToListResponse()).ToList();
        }
        #endregion

        #region Request to Entity
        public static Plant ToEntity(this PlantRequestDto request)
        {
            if (request == null) return null!;

            return new Plant
            {
                Name = request.Name,
                SpecificName = request.SpecificName,
                Origin = request.Origin,
                Description = request.Description,
                BasePrice = request.BasePrice,
                Placement = request.Placement,
                Size = request.Size,
                MinHeight = request.MinHeight,
                MaxHeight = request.MaxHeight,
                GrowthRate = request.GrowthRate,
                Toxicity = request.Toxicity,
                AirPurifying = request.AirPurifying,
                HasFlower = request.HasFlower,
                FengShuiElement = request.FengShuiElement,
                FengShuiMeaning = request.FengShuiMeaning,
                PotIncluded = request.PotIncluded,
                PotSize = request.PotSize,
                PlantType = request.PlantType,
                CareLevel = request.CareLevel,
                IsActive = request.IsActive,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }
        #endregion

        #region Update Entity
        public static void ToUpdate(this PlantUpdateDto request, Plant plant)
        {
            if (request == null || plant == null) return;

            plant.Name = request.Name;
            plant.SpecificName = request.SpecificName;
            plant.Origin = request.Origin;
            plant.Description = request.Description;
            plant.BasePrice = request.BasePrice;
            plant.Placement = request.Placement;
            plant.Size = request.Size;
            plant.MinHeight = request.MinHeight;
            plant.MaxHeight = request.MaxHeight;
            plant.GrowthRate = request.GrowthRate;
            plant.Toxicity = request.Toxicity;
            plant.AirPurifying = request.AirPurifying;
            plant.HasFlower = request.HasFlower;
            plant.FengShuiElement = request.FengShuiElement;
            plant.FengShuiMeaning = request.FengShuiMeaning;
            plant.PotIncluded = request.PotIncluded;
            plant.PotSize = request.PotSize;
            plant.PlantType = request.PlantType;
            plant.CareLevel = request.CareLevel;
            plant.IsActive = request.IsActive ?? plant.IsActive;
            plant.UpdatedAt = DateTime.Now;
        }
        #endregion


    }
}
