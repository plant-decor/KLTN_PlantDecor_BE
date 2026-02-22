using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Updates
{
    public class PlantUpdateDto
    {
        [Required(ErrorMessage = "Tên cây là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên cây không được vượt quá 200 ký tự")]
        public string Name { get; set; } = null!;

        public string? SpecificName { get; set; }

        public string? Origin { get; set; }

        public string? Description { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        public decimal? BasePrice { get; set; }

        public string? Placement { get; set; }

        public string? Size { get; set; }

        public int? MinHeight { get; set; }

        public int? MaxHeight { get; set; }

        public string? GrowthRate { get; set; }

        public bool? Toxicity { get; set; }

        public bool? AirPurifying { get; set; }

        public bool? HasFlower { get; set; }

        public string? FengShuiElement { get; set; }

        public string? FengShuiMeaning { get; set; }

        public bool? PotIncluded { get; set; }

        public string? PotSize { get; set; }

        public string? PlantType { get; set; }

        public string? CareLevel { get; set; }

        public string? Texture { get; set; }

        public bool? IsActive { get; set; }
    }
}
