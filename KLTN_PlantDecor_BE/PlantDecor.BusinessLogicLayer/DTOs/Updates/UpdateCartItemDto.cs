using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Updates
{
    public class UpdateCartItemDto
    {
        [Required, Range(1, int.MaxValue, ErrorMessage = "Quantity must > 0")]
        public int Quantity { get; set; }
    }
}
