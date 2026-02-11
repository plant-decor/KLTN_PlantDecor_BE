using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class LoginRequest
    {
        [Required(ErrorMessage = "Cần nhập Email")]
        [EmailAddress(ErrorMessage = "Sai định dạng email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Cần nhập mật khẩu")]
        public string Password { get; set; }
    }
}
