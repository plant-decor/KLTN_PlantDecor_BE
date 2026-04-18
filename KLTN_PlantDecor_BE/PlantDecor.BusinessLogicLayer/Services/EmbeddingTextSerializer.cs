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

            // Basic info
            sb.AppendLine($"Loại sản phẩm: Cây cảnh phổ thông");
            sb.AppendLine($"Tên cây: {dto.PlantName}");

            if (!string.IsNullOrEmpty(dto.PlantSpecificName))
                sb.AppendLine($"Tên khoa học: {dto.PlantSpecificName}");

            if (!string.IsNullOrEmpty(dto.PlantDescription))
                sb.AppendLine($"Mô tả: {dto.PlantDescription}");

            if (!string.IsNullOrEmpty(dto.PlantOrigin))
                sb.AppendLine($"Nguồn gốc: {dto.PlantOrigin}");

            // Feng Shui info
            if (dto.FengShuiElement.HasValue)
                sb.AppendLine($"Mệnh phong thủy: {GetFengShuiElementName(dto.FengShuiElement.Value)}");

            if (!string.IsNullOrEmpty(dto.FengShuiMeaning))
                sb.AppendLine($"Ý nghĩa phong thủy: {dto.FengShuiMeaning}");

            // Size and type
            if (dto.Size.HasValue)
                sb.AppendLine($"Kích thước: {GetSizeDescription(dto.Size.Value)}");

            sb.AppendLine($"Vị trí: {GetPlacementDescription(dto.PlacementType)}");

            if (dto.RoomTypeNames.Any())
                sb.AppendLine($"Không gian phù hợp: {string.Join(", ", dto.RoomTypeNames)}");

            if (dto.RoomStyleNames.Any())
                sb.AppendLine($"Phong cách phù hợp: {string.Join(", ", dto.RoomStyleNames)}");

            // Safety info
            var safetyFeatures = new List<string>();
            if (dto.PetSafe == true) safetyFeatures.Add("An toàn cho thú cưng");
            if (dto.ChildSafe == true) safetyFeatures.Add("An toàn cho trẻ em");
            if (dto.AirPurifying == true) safetyFeatures.Add("Lọc không khí");
            if (safetyFeatures.Any())
                sb.AppendLine($"Đặc điểm: {string.Join(", ", safetyFeatures)}");

            // Categories
            if (dto.CategoryNames.Any())
                sb.AppendLine($"Danh mục: {string.Join(", ", dto.CategoryNames)}");

            // Tags
            if (dto.TagNames.Any())
                sb.AppendLine($"Từ khóa: {string.Join(", ", dto.TagNames)}");

            // Price
            if (dto.Price.HasValue || dto.BasePrice.HasValue)
                sb.AppendLine($"Giá: {(dto.Price ?? dto.BasePrice):N0} VND");

            // Nursery
            if (!string.IsNullOrEmpty(dto.NurseryName))
                sb.AppendLine($"Vựa: {dto.NurseryName}");

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

            sb.AppendLine($"Loại sản phẩm: Cây độc bản (cây đơn lẻ, unique)");

            sb.AppendLine($"Tên cây: {dto.PlantName}");

            if (!string.IsNullOrEmpty(dto.PlantSpecificName))
                sb.AppendLine($"Tên khoa học: {dto.PlantSpecificName}");

            // Feng Shui
            if (dto.FengShuiElement.HasValue)
                sb.AppendLine($"Mệnh phong thủy: {GetFengShuiElementName(dto.FengShuiElement.Value)}");

            if (!string.IsNullOrEmpty(dto.FengShuiMeaning))
                sb.AppendLine($"Ý nghĩa phong thủy: {dto.FengShuiMeaning}");

            // Instance specific info
            if (!string.IsNullOrEmpty(dto.Description))
                sb.AppendLine($"Mô tả: {dto.Description}");

            if (!string.IsNullOrEmpty(dto.HealthStatus))
                sb.AppendLine($"Tình trạng sức khỏe: {dto.HealthStatus}");

            if (dto.Height.HasValue)
                sb.AppendLine($"Chiều cao: {dto.Height} cm");

            if (dto.TrunkDiameter.HasValue)
                sb.AppendLine($"Đường kính thân: {dto.TrunkDiameter} cm");

            if (dto.Age.HasValue)
                sb.AppendLine($"Tuổi cây: {dto.Age} năm");

            if (!string.IsNullOrEmpty(dto.SKU))
                sb.AppendLine($"Mã SKU: {dto.SKU}");

            // Price
            if (dto.Price.HasValue || dto.SpecificPrice.HasValue || dto.BasePrice.HasValue)
                sb.AppendLine($"Giá: {(dto.Price ?? dto.SpecificPrice ?? dto.BasePrice):N0} VND");

            if (dto.RoomTypeNames.Any())
                sb.AppendLine($"Không gian phù hợp: {string.Join(", ", dto.RoomTypeNames)}");

            if (dto.RoomStyleNames.Any())
                sb.AppendLine($"Phong cách phù hợp: {string.Join(", ", dto.RoomStyleNames)}");

            // Safety from plant
            var safetyFeatures = new List<string>();
            if (dto.PetSafe == true) safetyFeatures.Add("An toàn cho thú cưng");
            if (dto.ChildSafe == true) safetyFeatures.Add("An toàn cho trẻ em");
            if (dto.AirPurifying == true) safetyFeatures.Add("Lọc không khí");
            if (safetyFeatures.Any())
                sb.AppendLine($"Đặc điểm: {string.Join(", ", safetyFeatures)}");

            // Categories & Tags
            if (dto.CategoryNames.Any())
                sb.AppendLine($"Danh mục: {string.Join(", ", dto.CategoryNames)}");

            if (dto.TagNames.Any())
                sb.AppendLine($"Từ khóa: {string.Join(", ", dto.TagNames)}");

            // Nursery
            if (!string.IsNullOrEmpty(dto.NurseryName))
                sb.AppendLine($"Vựa: {dto.NurseryName}");

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

            sb.AppendLine($"Loại sản phẩm: Combo cây cảnh");

            if (!string.IsNullOrEmpty(dto.ComboName))
                sb.AppendLine($"Tên combo: {dto.ComboName}");

            if (!string.IsNullOrEmpty(dto.Description))
                sb.AppendLine($"Mô tả: {dto.Description}");

            // Suitable space
            if (!string.IsNullOrEmpty(dto.SuitableSpace))
                sb.AppendLine($"Không gian phù hợp: {dto.SuitableSpace}");

            if (dto.SuitableRooms.Any())
                sb.AppendLine($"Phòng phù hợp: {string.Join(", ", dto.SuitableRooms)}");

            // Feng Shui
            if (dto.FengShuiElement.HasValue)
                sb.AppendLine($"Mệnh phong thủy: {GetFengShuiElementName(dto.FengShuiElement.Value)}");

            if (!string.IsNullOrEmpty(dto.FengShuiPurpose))
                sb.AppendLine($"Mục đích phong thủy: {dto.FengShuiPurpose}");

            // Theme
            if (!string.IsNullOrEmpty(dto.ThemeName))
                sb.AppendLine($"Chủ đề: {dto.ThemeName}");

            if (!string.IsNullOrEmpty(dto.ThemeDescription))
                sb.AppendLine($"Mô tả chủ đề: {dto.ThemeDescription}");

            // Season
            if (dto.Season.HasValue)
                sb.AppendLine($"Mùa: {GetSeasonName(dto.Season.Value)}");

            // Safety
            var safetyFeatures = new List<string>();
            if (dto.PetSafe == true) safetyFeatures.Add("An toàn cho thú cưng");
            if (dto.ChildSafe == true) safetyFeatures.Add("An toàn cho trẻ em");
            if (safetyFeatures.Any())
                sb.AppendLine($"Đặc điểm: {string.Join(", ", safetyFeatures)}");

            // Price
            if (dto.Price.HasValue || dto.ComboPrice.HasValue)
                sb.AppendLine($"Giá combo: {(dto.Price ?? dto.ComboPrice):N0} VND");

            // Tags
            if (dto.TagNames.Any())
                sb.AppendLine($"Từ khóa: {string.Join(", ", dto.TagNames)}");

            // Nursery
            if (!string.IsNullOrEmpty(dto.NurseryName))
                sb.AppendLine($"Vựa: {dto.NurseryName}");

            return sb.ToString().Trim();
        }

        public string SerializeNurseryMaterial(NurseryMaterialEmbeddingDto dto)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"Loại sản phẩm: Vật tư làm vườn");

            if (!string.IsNullOrEmpty(dto.MaterialName))
                sb.AppendLine($"Tên sản phẩm: {dto.MaterialName}");

            if (!string.IsNullOrEmpty(dto.Description))
                sb.AppendLine($"Mô tả: {dto.Description}");

            if (!string.IsNullOrEmpty(dto.Brand))
                sb.AppendLine($"Thương hiệu: {dto.Brand}");

            if (!string.IsNullOrEmpty(dto.Unit))
                sb.AppendLine($"Đơn vị: {dto.Unit}");

            if (!string.IsNullOrEmpty(dto.Specifications))
                sb.AppendLine($"Thông số kỹ thuật: {dto.Specifications}");

            // Price
            if (dto.Price.HasValue || dto.BasePrice.HasValue)
                sb.AppendLine($"Giá: {(dto.Price ?? dto.BasePrice):N0} VND");

            // Categories
            if (dto.CategoryNames.Any())
                sb.AppendLine($"Danh mục: {string.Join(", ", dto.CategoryNames)}");

            // Tags
            if (dto.TagNames.Any())
                sb.AppendLine($"Từ khóa: {string.Join(", ", dto.TagNames)}");

            // Expiry info
            if (dto.ExpiredDate.HasValue)
                sb.AppendLine($"Hạn sử dụng: {dto.ExpiredDate.Value:dd/MM/yyyy}");

            // Nursery
            if (!string.IsNullOrEmpty(dto.NurseryName))
                sb.AppendLine($"Vựa: {dto.NurseryName}");

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
                1 => "Nhỏ (để bàn)",
                2 => "Vừa",
                3 => "Lớn",
                4 => "Rất lớn (cây cổ thụ)",
                _ => "Không xác định"
            };
        }

        private static string GetPlacementDescription(int placementType)
        {
            return placementType switch
            {
                1 => "Trong nhà",
                2 => "Ngoài trời",
                3 => "Cả hai (trong nhà và ngoài trời)",
                _ => "Không xác định"
            };
        }

        private static string GetFengShuiElementName(int fengShuiElement)
        {
            return Enum.IsDefined(typeof(FengShuiElementTypeEnum), fengShuiElement)
                ? ((FengShuiElementTypeEnum)fengShuiElement).ToString()
                : "Không xác định";
        }

        private static string GetSeasonName(SeasonTypeEnum season)
        {
            return season switch
            {
                SeasonTypeEnum.All => "Quanh năm",
                SeasonTypeEnum.Spring => "Mùa xuân",
                SeasonTypeEnum.Summer => "Mùa hè",
                SeasonTypeEnum.Autumn => "Mùa thu",
                SeasonTypeEnum.Winter => "Mùa đông",
                SeasonTypeEnum.Tet => "Tết",
                _ => "Không xác định"
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

            sb.AppendLine("Hướng dẫn chăm sóc:");

            if (!string.IsNullOrWhiteSpace(lightRequirementName))
            {
                sb.AppendLine($"Ánh sáng: {lightRequirementName}");
            }
            else if (lightRequirement.HasValue)
            {
                sb.AppendLine($"Ánh sáng (mức): {lightRequirement.Value}");
            }

            if (!string.IsNullOrWhiteSpace(watering))
                sb.AppendLine($"Tưới nước: {watering}");

            if (!string.IsNullOrWhiteSpace(fertilizing))
                sb.AppendLine($"Bón phân: {fertilizing}");

            if (!string.IsNullOrWhiteSpace(pruning))
                sb.AppendLine($"Cắt tỉa: {pruning}");

            if (!string.IsNullOrWhiteSpace(temperature))
                sb.AppendLine($"Nhiệt độ: {temperature}");

            if (!string.IsNullOrWhiteSpace(humidity))
                sb.AppendLine($"Độ ẩm: {humidity}");

            if (!string.IsNullOrWhiteSpace(soil))
                sb.AppendLine($"Đất trồng: {soil}");

            if (!string.IsNullOrWhiteSpace(careNotes))
                sb.AppendLine($"Ghi chú chăm sóc: {careNotes}");
        }
    }
}
