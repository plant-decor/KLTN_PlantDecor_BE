using PlantDecor.BusinessLogicLayer.DTOs.Embedding;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Enums;
using System.Text;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class EmbeddingTextSerializer : IEmbeddingTextSerializer
    {
        public string SerializeCommonPlant(CommonPlantEmbeddingDto dto)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Product type: Common plant");
            sb.AppendLine($"Plant name: {dto.PlantName}");

            if (!string.IsNullOrEmpty(dto.PlantSpecificName))
                sb.AppendLine($"Scientific name: {dto.PlantSpecificName}");

            if (!string.IsNullOrEmpty(dto.PlantDescription))
                sb.AppendLine($"Description: {dto.PlantDescription}");

            if (!string.IsNullOrEmpty(dto.PlantOrigin))
                sb.AppendLine($"Origin: {dto.PlantOrigin}");

            if (dto.FengShuiElement.HasValue)
                sb.AppendLine($"Feng Shui element: {GetFengShuiElementName(dto.FengShuiElement.Value)}");

            if (!string.IsNullOrEmpty(dto.FengShuiMeaning))
                sb.AppendLine($"Feng Shui meaning: {dto.FengShuiMeaning}");

            if (dto.Size.HasValue)
                sb.AppendLine($"Size: {GetSizeDescription(dto.Size.Value)}");

            sb.AppendLine($"Placement: {GetPlacementDescription(dto.PlacementType)}");

            if (dto.RoomTypeNames.Any())
                sb.AppendLine($"Suitable spaces: {string.Join(", ", dto.RoomTypeNames)}");

            if (dto.RoomStyleNames.Any())
                sb.AppendLine($"Suitable styles: {string.Join(", ", dto.RoomStyleNames)}");

            var safetyFeatures = new List<string>();
            if (dto.PetSafe == true) safetyFeatures.Add("Pet-safe");
            if (dto.ChildSafe == true) safetyFeatures.Add("Child-safe");
            if (dto.AirPurifying == true) safetyFeatures.Add("Air purifying");
            if (safetyFeatures.Any())
                sb.AppendLine($"Features: {string.Join(", ", safetyFeatures)}");

            if (dto.CategoryNames.Any())
                sb.AppendLine($"Categories: {string.Join(", ", dto.CategoryNames)}");

            if (dto.TagNames.Any())
                sb.AppendLine($"Tags: {string.Join(", ", dto.TagNames)}");

            if (dto.Price.HasValue || dto.BasePrice.HasValue)
                sb.AppendLine($"Price: {(dto.Price ?? dto.BasePrice):N0} VND");

            if (!string.IsNullOrEmpty(dto.NurseryName))
                sb.AppendLine($"Nursery: {dto.NurseryName}");

            AppendPlantGuideSection(
                sb,
                dto.GuideLightRequirementName,
                dto.GuideLightRequirement,
                dto.GuideWatering,
                dto.GuideFertilizing,
                dto.GuidePruning,
                dto.GuideTemperature,
                dto.GuideHumidity,
                dto.GuideSoil,
                dto.GuideCareNotes);

            return sb.ToString().Trim();
        }

        public string SerializePlantInstance(PlantInstanceEmbeddingDto dto)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Product type: Unique plant (single specimen)");

            sb.AppendLine($"Plant name: {dto.PlantName}");

            if (!string.IsNullOrEmpty(dto.PlantSpecificName))
                sb.AppendLine($"Scientific name: {dto.PlantSpecificName}");

            if (dto.FengShuiElement.HasValue)
                sb.AppendLine($"Feng Shui element: {GetFengShuiElementName(dto.FengShuiElement.Value)}");

            if (!string.IsNullOrEmpty(dto.FengShuiMeaning))
                sb.AppendLine($"Feng Shui meaning: {dto.FengShuiMeaning}");

            if (!string.IsNullOrEmpty(dto.Description))
                sb.AppendLine($"Description: {dto.Description}");

            if (!string.IsNullOrEmpty(dto.HealthStatus))
                sb.AppendLine($"Health status: {dto.HealthStatus}");

            if (dto.Height.HasValue)
                sb.AppendLine($"Height: {dto.Height} cm");

            if (dto.TrunkDiameter.HasValue)
                sb.AppendLine($"Trunk diameter: {dto.TrunkDiameter} cm");

            if (dto.Age.HasValue)
                sb.AppendLine($"Plant age: {dto.Age} years");

            if (!string.IsNullOrEmpty(dto.SKU))
                sb.AppendLine($"SKU: {dto.SKU}");

            if (dto.Price.HasValue || dto.SpecificPrice.HasValue || dto.BasePrice.HasValue)
                sb.AppendLine($"Price: {(dto.Price ?? dto.SpecificPrice ?? dto.BasePrice):N0} VND");

            if (dto.RoomTypeNames.Any())
                sb.AppendLine($"Suitable spaces: {string.Join(", ", dto.RoomTypeNames)}");

            if (dto.RoomStyleNames.Any())
                sb.AppendLine($"Suitable styles: {string.Join(", ", dto.RoomStyleNames)}");

            var safetyFeatures = new List<string>();
            if (dto.PetSafe == true) safetyFeatures.Add("Pet-safe");
            if (dto.ChildSafe == true) safetyFeatures.Add("Child-safe");
            if (dto.AirPurifying == true) safetyFeatures.Add("Air purifying");
            if (safetyFeatures.Any())
                sb.AppendLine($"Features: {string.Join(", ", safetyFeatures)}");

            if (dto.CategoryNames.Any())
                sb.AppendLine($"Categories: {string.Join(", ", dto.CategoryNames)}");

            if (dto.TagNames.Any())
                sb.AppendLine($"Tags: {string.Join(", ", dto.TagNames)}");

            if (!string.IsNullOrEmpty(dto.NurseryName))
                sb.AppendLine($"Nursery: {dto.NurseryName}");

            AppendPlantGuideSection(
                sb,
                dto.GuideLightRequirementName,
                dto.GuideLightRequirement,
                dto.GuideWatering,
                dto.GuideFertilizing,
                dto.GuidePruning,
                dto.GuideTemperature,
                dto.GuideHumidity,
                dto.GuideSoil,
                dto.GuideCareNotes);

            return sb.ToString().Trim();
        }

        public string SerializeNurseryPlantCombo(NurseryPlantComboEmbeddingDto dto)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Product type: Plant combo");

            if (!string.IsNullOrEmpty(dto.ComboName))
                sb.AppendLine($"Combo name: {dto.ComboName}");

            if (!string.IsNullOrEmpty(dto.Description))
                sb.AppendLine($"Description: {dto.Description}");

            if (!string.IsNullOrEmpty(dto.SuitableSpace))
                sb.AppendLine($"Suitable space: {dto.SuitableSpace}");

            if (dto.SuitableRooms.Any())
                sb.AppendLine($"Suitable rooms: {string.Join(", ", dto.SuitableRooms)}");

            if (dto.FengShuiElement.HasValue)
                sb.AppendLine($"Feng Shui element: {GetFengShuiElementName(dto.FengShuiElement.Value)}");

            if (!string.IsNullOrEmpty(dto.FengShuiPurpose))
                sb.AppendLine($"Feng Shui purpose: {dto.FengShuiPurpose}");

            if (!string.IsNullOrEmpty(dto.ThemeName))
                sb.AppendLine($"Theme: {dto.ThemeName}");

            if (!string.IsNullOrEmpty(dto.ThemeDescription))
                sb.AppendLine($"Theme description: {dto.ThemeDescription}");

            if (dto.Season.HasValue)
                sb.AppendLine($"Season: {GetSeasonName(dto.Season.Value)}");

            var safetyFeatures = new List<string>();
            if (dto.PetSafe == true) safetyFeatures.Add("Pet-safe");
            if (dto.ChildSafe == true) safetyFeatures.Add("Child-safe");
            if (safetyFeatures.Any())
                sb.AppendLine($"Features: {string.Join(", ", safetyFeatures)}");

            if (dto.Price.HasValue || dto.ComboPrice.HasValue)
                sb.AppendLine($"Combo price: {(dto.Price ?? dto.ComboPrice):N0} VND");

            if (dto.TagNames.Any())
                sb.AppendLine($"Tags: {string.Join(", ", dto.TagNames)}");

            if (!string.IsNullOrEmpty(dto.NurseryName))
                sb.AppendLine($"Nursery: {dto.NurseryName}");

            return sb.ToString().Trim();
        }

        public string SerializeNurseryMaterial(NurseryMaterialEmbeddingDto dto)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Product type: Gardening material");

            if (!string.IsNullOrEmpty(dto.MaterialName))
                sb.AppendLine($"Product name: {dto.MaterialName}");

            if (!string.IsNullOrEmpty(dto.Description))
                sb.AppendLine($"Description: {dto.Description}");

            if (!string.IsNullOrEmpty(dto.Brand))
                sb.AppendLine($"Brand: {dto.Brand}");

            if (!string.IsNullOrEmpty(dto.Unit))
                sb.AppendLine($"Unit: {dto.Unit}");

            if (!string.IsNullOrEmpty(dto.Specifications))
                sb.AppendLine($"Specifications: {dto.Specifications}");

            if (dto.Price.HasValue || dto.BasePrice.HasValue)
                sb.AppendLine($"Price: {(dto.Price ?? dto.BasePrice):N0} VND");

            if (dto.CategoryNames.Any())
                sb.AppendLine($"Categories: {string.Join(", ", dto.CategoryNames)}");

            if (dto.TagNames.Any())
                sb.AppendLine($"Tags: {string.Join(", ", dto.TagNames)}");

            if (dto.ExpiredDate.HasValue)
                sb.AppendLine($"Expiry date: {dto.ExpiredDate.Value:dd/MM/yyyy}");

            if (!string.IsNullOrEmpty(dto.NurseryName))
                sb.AppendLine($"Nursery: {dto.NurseryName}");

            return sb.ToString().Trim();
        }

        public Dictionary<string, object> ExtractMetadata(int nurseryId, decimal? price, string status, int originalEntityId)
        {
            return new Dictionary<string, object>
            {
                ["NurseryId"] = nurseryId,
                ["Price"] = price ?? 0m,
                ["Status"] = status,
                ["OriginalEntityId"] = originalEntityId,
                ["CreatedAt"] = DateTime.UtcNow.ToString("O")
            };
        }

        private static string GetSizeDescription(int size)
        {
            return size switch
            {
                1 => "Small (desktop)",
                2 => "Medium",
                3 => "Large",
                4 => "Very large (mature tree)",
                _ => "Unknown"
            };
        }

        private static string GetPlacementDescription(int placementType)
        {
            return placementType switch
            {
                1 => "Indoor",
                2 => "Outdoor",
                3 => "Both indoor and outdoor",
                _ => "Unknown"
            };
        }

        private static string GetFengShuiElementName(int fengShuiElement)
        {
            return Enum.IsDefined(typeof(FengShuiElementTypeEnum), fengShuiElement)
                ? ((FengShuiElementTypeEnum)fengShuiElement).ToString()
                : "Unknown";
        }

        private static string GetSeasonName(SeasonTypeEnum season)
        {
            return season switch
            {
                SeasonTypeEnum.All => "All year round",
                SeasonTypeEnum.Spring => "Spring",
                SeasonTypeEnum.Summer => "Summer",
                SeasonTypeEnum.Autumn => "Autumn",
                SeasonTypeEnum.Winter => "Winter",
                SeasonTypeEnum.Tet => "Tet",
                _ => "Unknown"
            };
        }

        private static void AppendPlantGuideSection(
            StringBuilder sb,
            string? lightRequirementName,
            int? lightRequirement,
            string? watering,
            string? fertilizing,
            string? pruning,
            string? temperature,
            string? humidity,
            string? soil,
            string? careNotes)
        {
            var hasGuideData =
                !string.IsNullOrWhiteSpace(lightRequirementName)
                || lightRequirement.HasValue
                || !string.IsNullOrWhiteSpace(watering)
                || !string.IsNullOrWhiteSpace(fertilizing)
                || !string.IsNullOrWhiteSpace(pruning)
                || !string.IsNullOrWhiteSpace(temperature)
                || !string.IsNullOrWhiteSpace(humidity)
                || !string.IsNullOrWhiteSpace(soil)
                || !string.IsNullOrWhiteSpace(careNotes);

            if (!hasGuideData)
            {
                return;
            }

            sb.AppendLine("Care guide:");

            if (!string.IsNullOrWhiteSpace(lightRequirementName))
            {
                sb.AppendLine($"Light: {lightRequirementName}");
            }
            else if (lightRequirement.HasValue)
            {
                sb.AppendLine($"Light level: {lightRequirement.Value}");
            }

            if (!string.IsNullOrWhiteSpace(watering))
                sb.AppendLine($"Watering: {watering}");

            if (!string.IsNullOrWhiteSpace(fertilizing))
                sb.AppendLine($"Fertilizing: {fertilizing}");

            if (!string.IsNullOrWhiteSpace(pruning))
                sb.AppendLine($"Pruning: {pruning}");

            if (!string.IsNullOrWhiteSpace(temperature))
                sb.AppendLine($"Temperature: {temperature}");

            if (!string.IsNullOrWhiteSpace(humidity))
                sb.AppendLine($"Humidity: {humidity}");

            if (!string.IsNullOrWhiteSpace(soil))
                sb.AppendLine($"Soil: {soil}");

            if (!string.IsNullOrWhiteSpace(careNotes))
                sb.AppendLine($"Care notes: {careNotes}");
        }
    }
}
