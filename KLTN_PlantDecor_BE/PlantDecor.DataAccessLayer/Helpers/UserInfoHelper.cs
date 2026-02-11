using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Helpers
{
    public class UserInfoHelper
    {
        public static int CalculateCompleteness(User user)
        {
            var fields = new object?[]
            {
        user.Username,
        user.PhoneNumber,
        user.AvatarUrl,
        user.UserProfile?.FullName,
        user.UserProfile?.Address,
        user.UserProfile?.BirthYear,
        user.UserProfile?.Gender
            };

            // Đếm số trường đã điền
            int filled = fields.Count(f => f != null && f.ToString() != string.Empty);
            // Tính phần trăm hoàn thiện
            return (int)((double)filled / fields.Length * 100);
        }
    }
}
