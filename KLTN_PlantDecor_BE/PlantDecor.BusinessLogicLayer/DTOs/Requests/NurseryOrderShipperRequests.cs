using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class StartShippingRequestDto
    {
        [StringLength(255)]
        public string? ShipperNote { get; set; }
    }

    public class MarkDeliveredRequestDto
    {
        [StringLength(255)]
        public string? DeliveryNote { get; set; }
    }

    public class MarkDeliveryFailedRequestDto
    {
        [Required(ErrorMessage = "Vui long nhap ly do giao hang that bai")]
        [StringLength(255)]
        public string FailureReason { get; set; } = null!;
    }
}
