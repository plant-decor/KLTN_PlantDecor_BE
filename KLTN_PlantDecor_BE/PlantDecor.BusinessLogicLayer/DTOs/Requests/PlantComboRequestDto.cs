using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class PlantComboRequestDto
    {
        public string? ComboCode { get; set; }

        [Required(ErrorMessage = "Tên combo là bắt buộc")]
        [StringLength(200, ErrorMessage = "Tên combo không được vượt quá 200 ký tự")]
        public string? ComboName { get; set; }

        public int? ComboType { get; set; }

        public string? Description { get; set; }

        public string? SuitableSpace { get; set; }

        public string? SuitableRooms { get; set; }

        public string? FengShuiElement { get; set; }

        public string? FengShuiPurpose { get; set; }

        public string? ThemeName { get; set; }

        public string? ThemeDescription { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá gốc phải lớn hơn hoặc bằng 0")]
        public decimal? OriginalPrice { get; set; }

        [Range(0, double.MaxValue, ErrorMessage = "Giá combo phải lớn hơn hoặc bằng 0")]
        public decimal? ComboPrice { get; set; }

        [Range(0, 100, ErrorMessage = "Phần trăm giảm giá phải từ 0 đến 100")]
        public decimal? DiscountPercent { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số cây tối thiểu phải lớn hơn 0")]
        public int? MinPlants { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số cây tối đa phải lớn hơn 0")]
        public int? MaxPlants { get; set; }

        public string? Tags { get; set; }

        public string? Season { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Danh sách các cây trong combo
        /// </summary>
        public List<PlantComboItemRequestDto> ComboItems { get; set; } = new List<PlantComboItemRequestDto>();
    }

    public class PlantComboItemRequestDto
    {
        [Required(ErrorMessage = "PlantId là bắt buộc")]
        public int PlantId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Số lượng cây phải lớn hơn 0")]
        public int Quantity { get; set; } = 1;

        public string? Notes { get; set; }
    }

    public class AssignComboTagsDto
    {
        [Required(ErrorMessage = "PlantComboId là bắt buộc")]
        public int PlantComboId { get; set; }

        [Required(ErrorMessage = "Danh sách TagIds là bắt buộc")]
        public List<int> TagIds { get; set; } = new List<int>();
    }
}
