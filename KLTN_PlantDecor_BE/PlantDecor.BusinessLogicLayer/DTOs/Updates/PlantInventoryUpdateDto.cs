using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Updates
{
    public class PlantInventoryUpdateDto
    {
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn hoặc bằng 0")]
        public int? Quantity { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Số lượng đã đặt trước phải lớn hơn hoặc bằng 0")]
        public int? ReservedQuantity { get; set; }
    }
}
