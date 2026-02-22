using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Updates
{
    public class PlantInstanceUpdateDto
    {
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn hoặc bằng 0")]
        public decimal? SpecificPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Chiều cao phải lớn hơn hoặc bằng 0")]
        public decimal? Height { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Đường kính thân phải lớn hơn hoặc bằng 0")]
        public decimal? TrunkDiameter { get; set; }

        public string? HealthStatus { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Tuổi phải lớn hơn hoặc bằng 0")]
        public int? Age { get; set; }

        public string? Description { get; set; }

        public string? Status { get; set; }
    }
}
