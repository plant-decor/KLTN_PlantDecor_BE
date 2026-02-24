using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class SecurityStampCacheService : ISecurityStampCacheService
    {
        private readonly ICacheService _cacheService;
        private readonly IUnitOfWork _unitOfWork;
        private const string PREFIX = "security_stamp:";
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(30);

        public SecurityStampCacheService(ICacheService cacheService, IUnitOfWork unitOfWork)
        {
            _cacheService = cacheService;
            _unitOfWork = unitOfWork;
        }

        /// <summary>
        /// Gọi khi đổi password, logout, revoke token...
        /// </summary>
        public async Task InvalidateSecurityStampAsync(int userId)
        {
            await _cacheService.RemoveDataAsync($"{PREFIX}{userId}");
        }

        /// <summary>
        /// Gọi khi login thành công, set stamp mới vào cache
        /// </summary>
        public async Task SetSecurityStampAsync(int userId, string stamp)
        {
            await _cacheService.SetDataAsync(
                                    $"{PREFIX}{userId}",
                                    stamp,
                                    DateTimeOffset.Now.Add(CACHE_DURATION)
                                            );
        }

        /// <summary>
        /// Validate SecurityStamp: Redis trước, DB sau
        /// </summary>
        public async Task<bool> ValidateSecurityStampAsync(int userId, string tokenStamp)
        {
            // 1. Check Redis
            var cachedStamp = await _cacheService.GetDataAsync<string>($"{PREFIX}{userId}");

            if (!string.IsNullOrEmpty(cachedStamp))
            {
                return cachedStamp == tokenStamp;
            }

            // 2. Cache miss → query DB
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.SecurityStamp)) return false;

            // 3. Set cache cho lần sau
            await SetSecurityStampAsync(userId, user.SecurityStamp);

            return user.SecurityStamp == tokenStamp;
        }
    }
}
