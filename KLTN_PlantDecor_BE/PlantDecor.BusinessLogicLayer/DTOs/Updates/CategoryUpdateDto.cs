using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Updates
{
    public class CategoryUpdateDto
    {
        [StringLength(100, ErrorMessage = "Tên category không được vượt quá 100 ký tự")]
        public string? Name { get; set; }

        public int? ParentCategoryId { get; set; }

        public bool? IsActive { get; set; }

        public int? CategoryType { get; set; }
    }
}
