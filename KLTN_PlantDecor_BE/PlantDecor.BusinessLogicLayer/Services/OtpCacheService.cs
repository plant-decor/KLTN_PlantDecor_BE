using PlantDecor.BusinessLogicLayer.DTOs.Models;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class OtpCacheService : IOtpCacheService
    {
        private readonly ICacheService _cacheService;
        private const string OTP_CACHE_PREFIX = "OTP";

        public OtpCacheService(ICacheService cacheService)
        {
            _cacheService = cacheService;
        }

        public async Task<bool> SaveOtpAsync(string email, string otpCode, string purpose, int userId, int expiryMinutes = 10)
        {
            try
            {
                var key = GenerateKey(email, purpose);
                var now = DateTime.UtcNow;
                var expiresAt = now.AddMinutes(expiryMinutes);

                var otpData = new OtpCacheModel
                {
                    OtpCode = otpCode,
                    Purpose = purpose,
                    CreatedAt = now,
                    ExpiresAt = expiresAt,
                    UserId = userId,
                    Email = email
                };

                // Use DateTimeOffset.UtcNow to avoid timezone issues with RedisCacheService
                var expirationTime = DateTimeOffset.Now.AddMinutes(expiryMinutes);
                await _cacheService.SetDataAsync(key, otpData, expirationTime);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<OtpCacheModel?> GetOtpAsync(string email, string purpose)
        {
            try
            {
                var key = GenerateKey(email, purpose);
                var otpData = await _cacheService.GetDataAsync<OtpCacheModel>(key);

                // Check if expired
                if (otpData != null && otpData.ExpiresAt < DateTime.UtcNow)
                {
                    await RemoveOtpAsync(email, purpose);
                    return null;
                }

                return otpData;
            }
            catch
            {
                return null;
            }
        }

        public async Task<bool> ValidateOtpAsync(string email, string otpCode, string purpose)
        {
            var otpData = await GetOtpAsync(email, purpose);

            if (otpData == null)
            {
                return false;
            }

            // Check if OTP matches and not expired
            if (otpData.OtpCode == otpCode && otpData.ExpiresAt > DateTime.UtcNow)
            {
                // Remove OTP after successful validation (one-time use)
                await RemoveOtpAsync(email, purpose);
                return true;
            }

            return false;
        }

        public async Task RemoveOtpAsync(string email, string purpose)
        {
            var key = GenerateKey(email, purpose);
            await _cacheService.RemoveDataAsync(key);
        }

        private string GenerateKey(string email, string purpose)
        {
            // Format: OTP:email@example.com:EmailVerification
            return $"{OTP_CACHE_PREFIX}:{email.ToLower()}:{purpose}";
        }
    }
}
