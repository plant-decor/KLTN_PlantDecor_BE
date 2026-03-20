using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class SendOtpEmailVerificationRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;
    }
}
