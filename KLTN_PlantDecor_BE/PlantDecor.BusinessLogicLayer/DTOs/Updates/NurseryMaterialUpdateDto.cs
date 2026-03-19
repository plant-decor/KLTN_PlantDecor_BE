using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Updates
{
    public class NurseryMaterialUpdateDto
    {
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0")]
        public int? Quantity { get; set; }

        public DateOnly? ExpiredDate { get; set; }
        public bool? IsActive { get; set; }
    }
}
