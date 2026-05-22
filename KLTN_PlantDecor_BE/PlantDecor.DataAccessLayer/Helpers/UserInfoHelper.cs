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
        // Count BirthDate instead of legacy BirthYear
        user.UserProfile?.BirthDate,
        user.UserProfile?.Gender
            };

            // Đếm số trường đã điền
            int filled = fields.Count(f => f != null && f.ToString() != string.Empty);
            // Tính phần trăm hoàn thiện
            return (int)((double)filled / fields.Length * 100);
        }

        // Compute feng shui element from birth year (copied logic from UserPreferenceService)
        public static int? GetFengShuiElementFromYear(int birthYear)
        {
            if (birthYear <= 0) return null;

            try
            {
                var mod10 = PositiveModNoDivision(birthYear, 10);
                var mod12 = PositiveModNoDivision(birthYear, 12);

                int canValue = mod10 switch
                {
                    0 or 1 => 4,
                    2 or 3 => 5,
                    4 or 5 => 1,
                    6 or 7 => 2,
                    8 or 9 => 3,
                    _ => 0
                };

                int chiValue = mod12 switch
                {
                    4 or 5 or 10 or 11 => 0,
                    6 or 7 or 0 or 1 => 1,
                    8 or 9 or 2 or 3 => 2,
                    _ => 0
                };

                int result = canValue + chiValue;
                if (result > 5) result -= 5;

                return result switch
                {
                    1 => (int)PlantDecor.DataAccessLayer.Enums.FengShuiElementTypeEnum.Metal,
                    2 => (int)PlantDecor.DataAccessLayer.Enums.FengShuiElementTypeEnum.Wood,
                    3 => (int)PlantDecor.DataAccessLayer.Enums.FengShuiElementTypeEnum.Water,
                    4 => (int)PlantDecor.DataAccessLayer.Enums.FengShuiElementTypeEnum.Fire,
                    5 => (int)PlantDecor.DataAccessLayer.Enums.FengShuiElementTypeEnum.Earth,
                    _ => null
                };
            }
            catch
            {
                return null;
            }
        }

        private static int PositiveModNoDivision(int value, int modulus)
        {
            if (modulus <= 0) return 0;
            var remainder = Math.Abs(value);
            while (remainder >= modulus) remainder -= modulus;
            return remainder;
        }
    }
}
