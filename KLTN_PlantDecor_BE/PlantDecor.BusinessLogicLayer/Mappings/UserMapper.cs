using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class UserMapper
    {
        #region Entity to Response
        public static UserResponse ToResponse(this User user)
        {
            if (user == null) return null;
            return new UserResponse
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Username = user.Username ?? string.Empty,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Status = (UserStatusEnum)user.Status,
                IsVerified = user.IsVerified,
                Role = (RoleEnum)user.RoleId,
                AvatarUrl = user.AvatarUrl,

                FullName = user.UserProfile?.FullName,
                Address = user.UserProfile?.Address,
                BirthYear = user.UserProfile?.BirthYear,
                Gender = user.UserProfile?.Gender,
                ReceiveNotifications = user.UserProfile?.ReceiveNotifications,
                ProfileCompleteness = user.UserProfile?.ProfileCompleteness
            };
        }
        #endregion

        #region Request to Entity
        public static User ToEntity(this UserRequest request)
        {
            if (request == null) return null;

            return new User
            {
                Email = request.Email.Trim(),
                PasswordHash = request.Password, // Hash ở service layer, không hash ở đây
                Username = request.Username?.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                RoleId = (int)request.RoleId,
                Status = (int)UserStatusEnum.Active,
                CreatedAt = DateTime.UtcNow,

                // Tạo UserProfile luôn
                UserProfile = new UserProfile
                {
                    FullName = request.FullName?.Trim(),
                    CreatedAt = DateTime.UtcNow
                }
            };
        }
        #endregion

        #region Request to Updated Entity (User tự cập nhật)
        public static void ToUpdate(this UserUpdate request, User user)
        {
            if (request == null || user == null) return;

            // Update User
            if (!string.IsNullOrWhiteSpace(request.UserName))
                user.Username = request.UserName.Trim();

            user.UpdatedAt = DateTime.UtcNow;

            // Update UserProfile (tạo mới nếu chưa có)
            user.UserProfile ??= new UserProfile
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            if (!string.IsNullOrWhiteSpace(request.FullName))
                user.UserProfile.FullName = request.FullName.Trim();

            if (!string.IsNullOrWhiteSpace(request.Address))
                user.UserProfile.Address = request.Address.Trim();

            if (request.BirthYear.HasValue)
                user.UserProfile.BirthYear = request.BirthYear;

            if (request.Gender.HasValue)
                user.UserProfile.Gender = (int)request.Gender;

            if (request.ReceiveNotifications.HasValue)
                user.UserProfile.ReceiveNotifications = request.ReceiveNotifications;

            user.UserProfile.UpdatedAt = DateTime.UtcNow;

            // Tính ProfileCompleteness
            user.UserProfile.ProfileCompleteness = UserInfoHelper.CalculateCompleteness(user);
        }
        #endregion

        #region Admin Update Entity
        public static void ToAdminUpdate(this AdminUserUpdate request, User user)
        {
            if (request == null || user == null) return;

            if (request.Role.HasValue)
                user.RoleId = (int)request.Role;

            if (request.Status.HasValue)
                user.Status = (int)request.Status;

            user.IsVerified = request.isVerified;

            user.UpdatedAt = DateTime.UtcNow;
        }
        #endregion


    }
}
