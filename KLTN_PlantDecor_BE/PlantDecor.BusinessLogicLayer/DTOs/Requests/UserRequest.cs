using PlantDecor.DataAccessLayer.Enums;
using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class UserRequest
    {
        [Required(ErrorMessage = "Cần nhập Email")]
        [EmailAddress(ErrorMessage = "Sai định dạng email")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cần nhập mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cần nhập lại mật khẩu lần nữa")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Cần nhập tên người dùng")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Cần nhập đầy đủ họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [RegularExpression(@"^(0|\+84)(\d{9})$", ErrorMessage = "Sai định dạng điện thoại")]
        public string? PhoneNumber { get; set; }
        [Required(ErrorMessage = "Cần có Role")]
        public RoleEnum RoleId { get; set; }


    }
}
