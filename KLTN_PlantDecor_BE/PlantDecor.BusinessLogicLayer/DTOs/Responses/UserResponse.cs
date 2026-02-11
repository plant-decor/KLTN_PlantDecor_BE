using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class UserResponse
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public UserStatusEnum? Status { get; set; }
        public bool? IsVerified { get; set; }
        public string? AvatarUrl { get; set; }
        public RoleEnum Role { get; set; }



        //UserProfile info
        public string? FullName { get; set; }
        public string? Address { get; set; }
        public int? BirthYear { get; set; }
        public int? Gender { get; set; }
        public bool? ReceiveNotifications { get; set; }
        public int? ProfileCompleteness { get; set; }
    }
}
