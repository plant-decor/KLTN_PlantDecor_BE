using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class PolicyKnowledgeService : IPolicyKnowledgeService
    {
        private const string PolicyCachePrefix = "policy_content";
        private const string PolicyAllActiveKey = "policy_content_all_active";

        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ILogger<PolicyKnowledgeService> _logger;
        private readonly int _cacheMinutes;

        public PolicyKnowledgeService(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IConfiguration configuration,
            ILogger<PolicyKnowledgeService> logger)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _logger = logger;
            _cacheMinutes = GetPositiveInt(configuration["AIChatbot:PolicyCacheMinutes"], 20);
        }

        public async Task<List<PolicyContent>> GetAllActiveAsync()
        {
            try
            {
                var cached = await _cacheService.GetDataAsync<List<PolicyContent>>(PolicyAllActiveKey);
                if (cached != null)
                {
                    return cached;
                }

                var policies = await _unitOfWork.PolicyContentRepository.GetAllActiveAsync();
                await _cacheService.SetDataAsync(PolicyAllActiveKey, policies, DateTimeOffset.Now.AddMinutes(_cacheMinutes));
                return policies;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Policy cache read failed for key {CacheKey}. Falling back to DB.", PolicyAllActiveKey);
                return await _unitOfWork.PolicyContentRepository.GetAllActiveAsync();
            }
        }

        public async Task<List<PolicyContent>> GetByCategoryActiveAsync(PolicyContentCategoryEnum category)
        {
            var cacheKey = GetCategoryCacheKey(category);

            try
            {
                var cached = await _cacheService.GetDataAsync<List<PolicyContent>>(cacheKey);
                if (cached != null)
                {
                    return cached;
                }

                var policies = await _unitOfWork.PolicyContentRepository.GetByCategoryActiveAsync((int)category);
                await _cacheService.SetDataAsync(cacheKey, policies, DateTimeOffset.Now.AddMinutes(_cacheMinutes));
                return policies;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Policy cache read failed for key {CacheKey}. Falling back to DB.", cacheKey);
                return await _unitOfWork.PolicyContentRepository.GetByCategoryActiveAsync((int)category);
            }
        }

        public async Task InvalidatePolicyCacheAsync()
        {
            try
            {
                await _cacheService.RemoveByPrefixAsync(PolicyCachePrefix);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Policy cache invalidation failed for prefix {CachePrefix}", PolicyCachePrefix);
            }
        }

        private static string GetCategoryCacheKey(PolicyContentCategoryEnum category)
        {
            return $"{PolicyCachePrefix}_category_{(int)category}_active";
        }

        private static int GetPositiveInt(string? value, int fallback)
        {
            return int.TryParse(value, out var parsed) && parsed > 0
                ? parsed
                : fallback;
        }
    }
}
