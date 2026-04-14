using System.ComponentModel.DataAnnotations;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Updates
{
    public class PlantGuideUpdateDto
    {
        [Range(1, int.MaxValue, ErrorMessage = "PlantId phải lớn hơn 0")]
        public int? PlantId { get; set; }

        [EnumDataType(typeof(LightRequirementEnum), ErrorMessage = "LightRequirement không hợp lệ")]
        public LightRequirementEnum? LightRequirement { get; set; }

        [StringLength(255, ErrorMessage = "Watering không được vượt quá 255 ký tự")]
        public string? Watering { get; set; }

        [StringLength(255, ErrorMessage = "Fertilizing không được vượt quá 255 ký tự")]
        public string? Fertilizing { get; set; }

        [StringLength(255, ErrorMessage = "Pruning không được vượt quá 255 ký tự")]
        public string? Pruning { get; set; }

        [StringLength(255, ErrorMessage = "Temperature không được vượt quá 255 ký tự")]
        public string? Temperature { get; set; }

        public string? Humidity { get; set; }

        public string? Soil { get; set; }

        public string? CareNotes { get; set; }
    }
}
