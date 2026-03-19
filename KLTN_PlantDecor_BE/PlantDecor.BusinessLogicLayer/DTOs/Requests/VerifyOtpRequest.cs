using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class VerifyOtpRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "OTP code is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        public string OtpCode { get; set; } = null!;

        [Required(ErrorMessage = "Purpose is required")]
        public string Purpose { get; set; } = null!; // "EmailVerification", "PasswordReset", "Login2FA"
    }
}
