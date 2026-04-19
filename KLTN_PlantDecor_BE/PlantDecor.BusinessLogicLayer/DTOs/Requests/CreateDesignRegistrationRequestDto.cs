using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CreateDesignRegistrationRequestDto
    {
        [Range(1, int.MaxValue)]
        public int? NurseryId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int DesignTemplateTierId { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        [Required]
        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^(0[3|5|7|8|9])[0-9]{8}$", ErrorMessage = "Số điện thoại không hợp lệ (phải là số điện thoại Việt Nam 10 chữ số)")]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? CustomerNote { get; set; }
    }
}
