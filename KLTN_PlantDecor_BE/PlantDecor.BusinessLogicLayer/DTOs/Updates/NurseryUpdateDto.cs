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

        [Range(-90.0, 90.0, ErrorMessage = "Vĩ độ phải nằm trong khoảng -90 đến 90")]
        public decimal? Latitude { get; set; }

        [Range(-180.0, 180.0, ErrorMessage = "Kinh độ phải nằm trong khoảng -180 đến 180")]
        public decimal? Longitude { get; set; }

        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$", ErrorMessage = "Số điện thoại không hợp lệ (phải là số điện thoại Việt Nam 10 chữ số)")]
        public string? Phone { get; set; }

        public bool? IsActive { get; set; }

        /// <summary>
        /// Gán manager cho vựa (chỉ dùng cho Admin)
        /// </summary>
        public int? ManagerId { get; set; }
    }
}
