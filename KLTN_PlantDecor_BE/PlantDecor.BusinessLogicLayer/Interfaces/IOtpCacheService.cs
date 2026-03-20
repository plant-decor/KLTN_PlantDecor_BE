using PlantDecor.BusinessLogicLayer.DTOs.Models;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IOtpCacheService
    {
        Task<bool> SaveOtpAsync(string email, string otpCode, string purpose, int userId, int expiryMinutes = 10);
        Task<OtpCacheModel?> GetOtpAsync(string email, string purpose);
        Task<bool> ValidateOtpAsync(string email, string otpCode, string purpose);
        Task RemoveOtpAsync(string email, string purpose);
    }
}
