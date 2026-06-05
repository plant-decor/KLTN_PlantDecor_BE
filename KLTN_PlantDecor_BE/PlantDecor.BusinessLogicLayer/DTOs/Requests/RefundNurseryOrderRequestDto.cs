using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class RefundNurseryOrderRequestDto
    {
        [Range(typeof(decimal), "0.01", "79228162514264337593543950335", ErrorMessage = "RefundedAmount must be greater than 0")]
        public decimal RefundedAmount { get; set; }

        [MaxLength(100)]
        public string? RefundReference { get; set; }

        [MaxLength(255)]
        public string? ManagerRefundNote { get; set; }
    }
}
