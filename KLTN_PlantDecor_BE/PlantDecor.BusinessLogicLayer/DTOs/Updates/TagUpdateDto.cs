using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Updates
{
    public class TagUpdateDto
    {
        [StringLength(50, ErrorMessage = "Tên tag không được vượt quá 50 ký tự")]
        public string? TagName { get; set; }

        public int? TagType { get; set; }
    }
}
