using PlantDecor.BusinessLogicLayer.DTOs.Embedding;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class EmbeddingBackfillMapper
    {
        public static CommonPlantEmbeddingDto ToEmbeddingBackfillDto(this CommonPlant entity)
        {
            var plant = entity.Plant;
            var guide = plant?.PlantGuide;

            return new CommonPlantEmbeddingDto
            {
                CommonPlantId = entity.Id,
                PlantId = entity.PlantId,
                IsActive = entity.IsActive,
                PlantName = plant?.Name ?? string.Empty,
                PlantSpecificName = plant?.SpecificName,
                PlantDescription = plant?.Description,
                PlantOrigin = plant?.Origin,
                FengShuiElement = plant?.FengShuiElement,
                FengShuiMeaning = plant?.FengShuiMeaning,
                Size = plant?.Size,
                PlacementType = plant?.PlacementType ?? 0,
                PetSafe = plant?.PetSafe,
                ChildSafe = plant?.ChildSafe,
                AirPurifying = plant?.AirPurifying,
                BasePrice = plant?.BasePrice,
                CategoryNames = plant?.Categories?
                    .Select(c => c.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToList() ?? new List<string>(),
                TagNames = plant?.Tags?
                    .Select(t => t.TagName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToList() ?? new List<string>(),
                NurseryId = entity.NurseryId,
                NurseryName = entity.Nursery?.Name,
                Price = plant?.BasePrice,
                GuideLightRequirement = guide?.LightRequirement,
                GuideLightRequirementName = GetLightRequirementName(guide?.LightRequirement),
                GuideWatering = guide?.Watering,
                GuideFertilizing = guide?.Fertilizing,
                GuidePruning = guide?.Pruning,
                GuideTemperature = guide?.Temperature,
                GuideHumidity = guide?.Humidity,
                GuideSoil = guide?.Soil,
                GuideCareNotes = guide?.CareNotes
            };
        }

        public static PlantInstanceEmbeddingDto ToEmbeddingBackfillDto(this PlantInstance entity)
        {
            var plant = entity.Plant;
            var guide = plant?.PlantGuide;

            return new PlantInstanceEmbeddingDto
            {
                PlantInstanceId = entity.Id,
                PlantId = entity.PlantId ?? 0,
                Status = entity.Status,
                PlantName = plant?.Name ?? string.Empty,
                PlantSpecificName = plant?.SpecificName,
                FengShuiElement = plant?.FengShuiElement,
                FengShuiMeaning = plant?.FengShuiMeaning,
                PetSafe = plant?.PetSafe,
                ChildSafe = plant?.ChildSafe,
                AirPurifying = plant?.AirPurifying,
                Description = entity.Description,
                HealthStatus = entity.HealthStatus,
                Height = entity.Height,
                TrunkDiameter = entity.TrunkDiameter,
                Age = entity.Age,
                SKU = entity.SKU,
                Price = entity.SpecificPrice,
                SpecificPrice = entity.SpecificPrice,
                BasePrice = plant?.BasePrice,
                CategoryNames = plant?.Categories?
                    .Select(c => c.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToList() ?? new List<string>(),
                TagNames = plant?.Tags?
                    .Select(t => t.TagName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToList() ?? new List<string>(),
                NurseryId = entity.CurrentNurseryId ?? 0,
                NurseryName = entity.CurrentNursery?.Name,
                GuideLightRequirement = guide?.LightRequirement,
                GuideLightRequirementName = GetLightRequirementName(guide?.LightRequirement),
                GuideWatering = guide?.Watering,
                GuideFertilizing = guide?.Fertilizing,
                GuidePruning = guide?.Pruning,
                GuideTemperature = guide?.Temperature,
                GuideHumidity = guide?.Humidity,
                GuideSoil = guide?.Soil,
                GuideCareNotes = guide?.CareNotes
            };
        }

        public static NurseryPlantComboEmbeddingDto ToEmbeddingBackfillDto(this NurseryPlantCombo entity)
        {
            var combo = entity.PlantCombo;

            return new NurseryPlantComboEmbeddingDto
            {
                NurseryPlantComboId = entity.Id,
                IsActive = entity.IsActive,
                ComboName = combo?.ComboName ?? string.Empty,
                Description = combo?.Description,
                SuitableSpace = GetLightRequirementName(combo?.SuitableSpace),
                SuitableRooms = combo?.SuitableRooms?.Select(GetRoomTypeName).ToList() ?? new List<string>(),
                FengShuiElement = combo?.FengShuiElement,
                FengShuiPurpose = combo?.FengShuiPurpose,
                ThemeName = combo?.ThemeName,
                ThemeDescription = combo?.ThemeDescription,
                Season = combo?.Season.HasValue == true && Enum.IsDefined(typeof(SeasonTypeEnum), combo.Season.Value)
                    ? (SeasonTypeEnum?)combo.Season.Value
                    : null,
                PetSafe = combo?.PetSafe,
                ChildSafe = combo?.ChildSafe,
                ComboPrice = combo?.ComboPrice,
                TagNames = combo?.TagsNavigation?
                    .Select(t => t.TagName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToList() ?? new List<string>(),
                NurseryId = entity.NurseryId,
                NurseryName = entity.Nursery?.Name,
                Price = combo?.ComboPrice
            };
        }

        public static NurseryMaterialEmbeddingDto ToEmbeddingBackfillDto(this NurseryMaterial entity)
        {
            var material = entity.Material;

            return new NurseryMaterialEmbeddingDto
            {
                NurseryMaterialId = entity.Id,
                IsActive = entity.IsActive,
                ExpiredDate = entity.ExpiredDate,
                MaterialName = material?.Name ?? string.Empty,
                Description = material?.Description,
                Brand = material?.Brand,
                Unit = material?.Unit,
                Specifications = material?.Specifications,
                BasePrice = material?.BasePrice,
                CategoryNames = material?.Categories?
                    .Select(c => c.Name)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToList() ?? new List<string>(),
                TagNames = material?.Tags?
                    .Select(t => t.TagName)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToList() ?? new List<string>(),
                NurseryId = entity.NurseryId,
                NurseryName = entity.Nursery?.Name,
                Price = material?.BasePrice
            };
        }

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

        private static string GetRoomTypeName(int roomType)
        {
            if (Enum.IsDefined(typeof(RoomTypeEnum), roomType))
            {
                return ((RoomTypeEnum)roomType).ToString();
            }

            return roomType.ToString();
        }
    }
}