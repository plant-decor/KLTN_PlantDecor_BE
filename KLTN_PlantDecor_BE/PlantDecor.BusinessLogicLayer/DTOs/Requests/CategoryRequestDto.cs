using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CategoryRequestDto
    {
        [Required(ErrorMessage = "Tên category là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên category không được vượt quá 100 ký tự")]
        public string Name { get; set; } = null!;

        public int? ParentCategoryId { get; set; }

        public bool IsActive { get; set; } = true;

        [Required(ErrorMessage = "Loại category là bắt buộc")]
        public int CategoryType { get; set; }
    }
}
