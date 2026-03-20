using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CartItemRequestDto
    {
        public int? CommonPlantId { get; set; }
        public int? NurseryPlantComboId { get; set; }
        public int? NurseryMaterialId { get; set; }

        [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must > 0")]
        public int Quantity { get; set; }
    }
}
