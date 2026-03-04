using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class NurseryRequestDto
    {
        [Required(ErrorMessage = "Tên vựa là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên vựa không được vượt quá 200 ký tự")]
        public string Name { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự")]
        public string? Address { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Diện tích phải lớn hơn hoặc bằng 0")]
        public decimal? Area { get; set; }

        public decimal? Latitude { get; set; }

        public decimal? Longitude { get; set; }

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? Phone { get; set; }

        /// <summary>
        /// Loại vựa: 1 = Indoor, 2 = Outdoor, 3 = Mixed
        /// </summary>
        public int? Type { get; set; }

        /// <summary>
        /// Điều kiện ánh sáng: 1 = Low, 2 = Medium, 3 = High
        /// </summary>
        public int? LightCondition { get; set; }

        /// <summary>
        /// Độ ẩm: 1 = Low, 2 = Medium, 3 = High
        /// </summary>
        public int? HumidityLevel { get; set; }

        public bool? HasMistSystem { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
