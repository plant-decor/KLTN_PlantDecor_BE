using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
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

            var plantLevelImages = plant.PlantImages
                .Where(i => i.PlantInstanceId == null)
                .ToList();

            return new PlantResponseDto
            {
                Id = plant.Id,
                Name = plant.Name,
                SpecificName = plant.SpecificName,
                Origin = plant.Origin,
                Description = plant.Description,
                BasePrice = plant.BasePrice,
                PlacementType = plant.PlacementType,
                PlacementTypeName = ((PlacementTypeEnum)plant.PlacementType).ToString(),
                RoomStyle = plant.RoomStyle,
                RoomStyleNames = GetEnumNames<RoomStyleEnum>(plant.RoomStyle),
                RoomType = plant.RoomType,
                RoomTypeNames = GetEnumNames<RoomTypeEnum>(plant.RoomType),
                Size = plant.Size,
                SizeName = GetPlantSizeName(plant.Size),
                GrowthRate = plant.GrowthRate,
                GrowthRateName = GetGrowthRateName(plant.GrowthRate),
                Toxicity = plant.Toxicity,
                AirPurifying = plant.AirPurifying,
                HasFlower = plant.HasFlower,
                PetSafe = plant.PetSafe,
                ChildSafe = plant.ChildSafe,
                FengShuiElement = plant.FengShuiElement,
                FengShuiElementName = GetFengShuiElementName(plant.FengShuiElement),
                FengShuiMeaning = plant.FengShuiMeaning,
                PotIncluded = plant.PotIncluded,
                PotSize = plant.PotSize,
                CareLevelType = plant.CareLevelType,
                CareLevelTypeName = GetCareLevelName(plant.CareLevelType),
                IsUniqueInstance = plant.IsUniqueInstance,
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
                Images = plantLevelImages.Select(i => new PlantImageResponseDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    IsPrimary = i.IsPrimary
                }).ToList(),
                // PlantInstances temporarily disabled
                TotalInstances = plant.PlantInstances.Count,
                AvailableInstances = plant.PlantInstances.Count(i => i.Status == (int)PlantInstanceStatusEnum.Available)
            };
        }

        public static PlantListResponseDto ToListResponse(this Plant plant)
        {
            if (plant == null) return null!;

            var availableInstances = plant.PlantInstances.Count(i => i.Status == (int)PlantInstanceStatusEnum.Available);
            var availableCommonQuantity = plant.CommonPlants
                .Where(cp => cp.IsActive && cp.Quantity > 0)
                .Sum(cp => cp.Quantity);
            var primaryImageUrl = plant.PlantImages
                .Where(i => i.PlantInstanceId == null && i.IsPrimary == true)
                .Select(i => i.ImageUrl)
                .FirstOrDefault();

            return new PlantListResponseDto
            {
                Id = plant.Id,
                Name = plant.Name,
                BasePrice = plant.BasePrice,
                IsUniqueInstance = plant.IsUniqueInstance,
                Size = plant.Size,
                SizeName = GetPlantSizeName(plant.Size),
                CareLevelType = plant.CareLevelType,
                CareLevelTypeName = GetCareLevelName(plant.CareLevelType),
                FengShuiElement = plant.FengShuiElement,
                FengShuiElementName = GetFengShuiElementName(plant.FengShuiElement),
                IsActive = plant.IsActive,
                PrimaryImageUrl = primaryImageUrl,
                TotalInstances = plant.PlantInstances.Count,
                AvailableInstances = availableInstances,
                AvailableCommonQuantity = availableCommonQuantity,
                TotalAvailableStock = availableInstances + availableCommonQuantity,
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
                PlacementType = request.PlacementType,
                RoomStyle = request.RoomStyle?.Distinct().ToList(),
                RoomType = request.RoomType?.Distinct().ToList(),
                Size = request.Size,
                GrowthRate = ResolveGrowthRate(request.GrowthRate),
                Toxicity = request.Toxicity,
                AirPurifying = request.AirPurifying,
                HasFlower = request.HasFlower,
                PetSafe = request.PetSafe,
                ChildSafe = request.ChildSafe,
                FengShuiElement = request.FengShuiElement,
                FengShuiMeaning = request.FengShuiMeaning,
                PotIncluded = request.PotIncluded,
                PotSize = request.PotSize,
                CareLevelType = request.CareLevelType,
                IsActive = request.IsActive,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                IsUniqueInstance = request.IsUniqueInstance
            };
        }
        #endregion

        #region Update Entity
        public static void ToUpdate(this PlantUpdateDto request, Plant plant)
        {
            if (request == null || plant == null) return;

            if (request.Name != null)
                plant.Name = request.Name;

            if (request.SpecificName != null)
                plant.SpecificName = request.SpecificName;

            if (request.Origin != null)
                plant.Origin = request.Origin;

            if (request.Description != null)
                plant.Description = request.Description;

            if (request.BasePrice.HasValue)
                plant.BasePrice = request.BasePrice.Value;

            if (request.Size != null)
                plant.Size = request.Size;

            if (request.GrowthRate.HasValue)
                plant.GrowthRate = ResolveGrowthRate(request.GrowthRate);

            if (request.PlacementType != null)
                plant.PlacementType = request.PlacementType.Value;

            if (request.RoomStyle != null)
                plant.RoomStyle = request.RoomStyle.Distinct().ToList();

            if (request.RoomType != null)
                plant.RoomType = request.RoomType.Distinct().ToList();

            if (request.Toxicity.HasValue)
                plant.Toxicity = request.Toxicity.Value;

            if (request.AirPurifying.HasValue)
                plant.AirPurifying = request.AirPurifying.Value;

            if (request.HasFlower.HasValue)
                plant.HasFlower = request.HasFlower.Value;

            if (request.PetSafe.HasValue)
                plant.PetSafe = request.PetSafe.Value;

            if (request.ChildSafe.HasValue)
                plant.ChildSafe = request.ChildSafe.Value;

            if (request.FengShuiElement != null)
                plant.FengShuiElement = request.FengShuiElement;

            if (request.FengShuiMeaning != null)
                plant.FengShuiMeaning = request.FengShuiMeaning;

            if (request.PotIncluded.HasValue)
                plant.PotIncluded = request.PotIncluded.Value;

            if (request.PotSize != null)
                plant.PotSize = request.PotSize;

            if (request.CareLevelType.HasValue || request.CareLevel != null)
                plant.CareLevelType = ResolveCareLevelType(request.CareLevelType, request.CareLevel);

            if (request.IsActive.HasValue)
                plant.IsActive = request.IsActive.Value;

            if (request.IsUniqueInstance.HasValue)
                plant.IsUniqueInstance = request.IsUniqueInstance.Value;

            plant.UpdatedAt = DateTime.Now;
        }
        #endregion

        private static int? ResolveCareLevelType(int? careLevelType, string? careLevel)
        {
            if (careLevelType.HasValue)
            {
                return careLevelType.Value;
            }

            if (string.IsNullOrWhiteSpace(careLevel))
            {
                return null;
            }

            return careLevel.Trim().ToLowerInvariant() switch
            {
                "easy" or "de" => (int)CareLevelTypeEnum.Easy,
                "medium" or "trungbinh" or "trung binh" => (int)CareLevelTypeEnum.Medium,
                "hard" or "kho" => (int)CareLevelTypeEnum.Hard,
                "expert" or "chuyengia" or "chuyen gia" => (int)CareLevelTypeEnum.Expert,
                _ => null
            };
        }

        private static string? GetCareLevelName(int? careLevelType)
        {
            if (!careLevelType.HasValue)
            {
                return null;
            }

            return Enum.IsDefined(typeof(CareLevelTypeEnum), careLevelType.Value)
                ? ((CareLevelTypeEnum)careLevelType.Value).ToString()
                : null;
        }

        private static string? GetPlantSizeName(int? size)
        {
            if (!size.HasValue)
            {
                return null;
            }

            return Enum.IsDefined(typeof(PlantSizeEnum), size.Value)
                ? ((PlantSizeEnum)size.Value).ToString()
                : null;
        }

        private static int ResolveGrowthRate(int? growthRate)
        {
            if (!growthRate.HasValue)
            {
                return (int)GrowthRateEnum.Moderate;
            }

            if (!Enum.IsDefined(typeof(GrowthRateEnum), growthRate.Value))
            {
                throw new BadRequestException("GrowthRate is invalid");
            }

            return growthRate.Value;
        }

        private static string? GetGrowthRateName(int growthRate)
        {
            return Enum.IsDefined(typeof(GrowthRateEnum), growthRate)
                ? ((GrowthRateEnum)growthRate).ToString()
                : null;
        }

        private static string? GetFengShuiElementName(int? fengShuiElement)
        {
            if (!fengShuiElement.HasValue)
            {
                return null;
            }

            return Enum.IsDefined(typeof(FengShuiElementTypeEnum), fengShuiElement.Value)
                ? ((FengShuiElementTypeEnum)fengShuiElement.Value).ToString()
                : null;
        }

        private static List<string>? GetEnumNames<TEnum>(List<int>? values) where TEnum : struct, Enum
        {
            if (values == null)
            {
                return null;
            }

            return values
                .Where(v => Enum.IsDefined(typeof(TEnum), v))
                .Select(v => ((TEnum)Enum.ToObject(typeof(TEnum), v)).ToString())
                .Distinct()
                .ToList();
        }


    }
}
