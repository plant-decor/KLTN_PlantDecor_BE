using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class StartShippingRequestDto
    {
        [StringLength(255, ErrorMessage = "ShipperNote không ???c v??t quá 255 ký t?")]
        public string? ShipperNote { get; set; }
    }

    public class MarkDeliveredRequestDto
    {
        [StringLength(255, ErrorMessage = "DeliveryNote không ???c v??t quá 255 ký t?")]
        public string? DeliveryNote { get; set; }
    }
}
