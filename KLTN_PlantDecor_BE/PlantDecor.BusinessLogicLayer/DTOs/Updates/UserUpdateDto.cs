using PlantDecor.DataAccessLayer.Enums;
using System.ComponentModel.DataAnnotations;

namespace PlantDecor.BusinessLogicLayer.DTOs.Updates
{
    public class UserUpdateDto
    {
        [Required(ErrorMessage = "UserName is required")]
        public string UserName { get; set; }

        [RegularExpression(@"^(0|\+84)(\d{9})$", ErrorMessage = "Sai định dạng điện thoại")]
        public string? PhoneNumber { get; set; }


        // User Profile
        [Required(ErrorMessage = "Full Name is required")]
        public string FullName { get; set; }
        public string? Address { get; set; }
        public int? BirthYear { get; set; }
        public GenderEnum? Gender { get; set; }           // 0: Nữ, 1: Nam, 2: Khác
        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }
        public bool? ReceiveNotifications { get; set; }
    }
}
