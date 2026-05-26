using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class MonthlyQuotaResetService : IMonthlyQuotaResetService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITierService _tierService;
        private readonly ILogger<MonthlyQuotaResetService> _logger;

        public MonthlyQuotaResetService(
            IUnitOfWork unitOfWork,
            ITierService tierService,
            ILogger<MonthlyQuotaResetService> logger)
        {
            _unitOfWork = unitOfWork;
            _tierService = tierService;
            _logger = logger;
        }

        // Called by Hangfire recurring job on the 1st of each month
        public async Task ResetMonthlyQuotaAsync()
        {
            _logger.LogInformation("MonthlyQuotaResetService: Starting monthly quota reset");

            var customerIds = await _unitOfWork.UserRepository.GetAllActiveCustomerIdsAsync();

            foreach (var userId in customerIds)
            {
                try
                {
                    // Recalculate tier first (may upgrade based on last month's spending)
                    await _tierService.RecalculateTierAsync(userId);

                    // Issue new free quota for this month
                    await GrantMonthlyFreeQuotaAsync(userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MonthlyQuotaResetService: Failed for UserId={UserId}", userId);
                }
            }

            _logger.LogInformation(
                "MonthlyQuotaResetService: Completed reset for {Count} customers", customerIds.Count);
        }

        // Also called on registration to grant the first month's free quota
        public async Task GrantMonthlyFreeQuotaAsync(int userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null) return;

            var threshold = await _unitOfWork.TierThresholdRepository.GetByTierLevelAsync(user.TierLevel);
            if (threshold == null || threshold.MonthlyFreeQuota <= 0)
            {
                _logger.LogInformation(
                    "MonthlyQuotaResetService: No free quota for UserId={UserId} TierLevel={Tier}",
                    userId, user.TierLevel);
                return;
            }

            // Deactivate previous monthly free subscriptions
            await _unitOfWork.UserSubscriptionRepository.DeactivateMonthlyFreeAsync(userId);

            // End of current month
            var now = DateTime.Now;
            var endOfMonth = new DateTime(now.Year, now.Month, 1).AddMonths(1).AddSeconds(-1);

            var sub = new UserSubscription
            {
                UserId = userId,
                TierPackageId = null,
                StartDate = now,
                EndDate = endOfMonth,
                IsActive = true,
                IsMonthlyFree = true,
                QuotaOverride = threshold.MonthlyFreeQuota,
                PaidAt = null,
                CreatedAt = now
            };

            _unitOfWork.UserSubscriptionRepository.PrepareCreate(sub);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation(
                "MonthlyQuotaResetService: Granted {Quota} free requests for UserId={UserId} (Tier={Tier}) until {End}",
                threshold.MonthlyFreeQuota, userId, threshold.Name, endOfMonth);
        }
    }
}
