using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class TagRequestDto
    {
        [Required(ErrorMessage = "Tên tag là bắt buộc")]
        [StringLength(50, ErrorMessage = "Tên tag không được vượt quá 50 ký tự")]
        public string TagName { get; set; } = null!;

        [Required(ErrorMessage = "Loại tag là bắt buộc")]
        public int TagType { get; set; }
    }
}
