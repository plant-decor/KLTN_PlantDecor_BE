using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CommonPlantRequestDto
    {
        [Required(ErrorMessage = "PlantId là bắt buộc")]
        public int PlantId { get; set; }

        [Required(ErrorMessage = "Số lượng là bắt buộc")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0")]
        public int Quantity { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
