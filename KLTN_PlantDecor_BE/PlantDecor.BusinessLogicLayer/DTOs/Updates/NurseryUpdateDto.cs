using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Updates
{
    public class NurseryUpdateDto
    {
        [StringLength(200, ErrorMessage = "Tên vựa không được vượt quá 200 ký tự")]
        public string? Name { get; set; }

        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        public string? Address { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Diện tích phải lớn hơn hoặc bằng 0")]
        public decimal? Area { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }

        public int? Type { get; set; }

        public int? LightCondition { get; set; }

        public int? HumidityLevel { get; set; }

        public bool? HasMistSystem { get; set; }

        public bool? IsActive { get; set; }
    }
}
