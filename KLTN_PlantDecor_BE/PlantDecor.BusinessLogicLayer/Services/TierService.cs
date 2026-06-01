using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class TierService : ITierService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<TierService> _logger;

        public TierService(IUnitOfWork unitOfWork, ILogger<TierService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task RecalculateTierAsync(int userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
            {
                _logger.LogWarning("TierService: User {UserId} not found", userId);
                return;
            }

            var totalSpent = await _unitOfWork.OrderRepository.SumCompletedOrderAmountByUserIdAsync(userId);

            var thresholds = await _unitOfWork.TierThresholdRepository.GetAllActiveAsync();

            // Find highest tier where total spent meets the minimum
            var matched = thresholds
                .Where(t => totalSpent >= t.MinTotalSpent)
                .OrderByDescending(t => t.TierLevel)
                .FirstOrDefault();

            if (matched == null) return;

            // No downgrade — only upgrade
            if (matched.TierLevel <= user.TierLevel) return;

            user.TierLevel = matched.TierLevel;
            _unitOfWork.UserRepository.PrepareUpdate(user);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation(
                "TierService: User {UserId} upgraded to tier {TierLevel} ({TierName}), totalSpent={TotalSpent}",
                userId, matched.TierLevel, matched.Name, totalSpent);
        }

        public async Task<TierProgressDto> GetTierProgressAsync(int userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            var totalSpent = await _unitOfWork.OrderRepository.SumCompletedOrderAmountByUserIdAsync(userId);

            var allThresholds = (await _unitOfWork.TierThresholdRepository.GetAllActiveAsync())
                .OrderBy(t => t.TierLevel)
                .ToList();

            var currentThreshold = allThresholds.FirstOrDefault(t => t.TierLevel == user.TierLevel)
                                   ?? allThresholds.FirstOrDefault();

            var nextThreshold = allThresholds.FirstOrDefault(t => t.TierLevel > user.TierLevel);

            bool isMax = nextThreshold == null;

            double progress;
            if (isMax)
            {
                progress = 100.0;
            }
            else
            {
                var currentMin = currentThreshold?.MinTotalSpent ?? 0m;
                var range = nextThreshold!.MinTotalSpent - currentMin;
                progress = range == 0 ? 100.0
                    : Math.Clamp((double)((totalSpent - currentMin) / range) * 100.0, 0.0, 100.0);
            }

            return new TierProgressDto
            {
                CurrentTierLevel = user.TierLevel,
                CurrentTierName = currentThreshold?.Name,
                CurrentTierBenefitDescription = currentThreshold?.BenefitDescription,
                CurrentTierMonthlyFreeQuota = currentThreshold?.MonthlyFreeQuota ?? 0,
                CurrentTierMinSpent = currentThreshold?.MinTotalSpent ?? 0m,

                TotalSpent = totalSpent,

                NextTierLevel = nextThreshold?.TierLevel,
                NextTierName = nextThreshold?.Name,
                NextTierBenefitDescription = nextThreshold?.BenefitDescription,
                NextTierMonthlyFreeQuota = nextThreshold?.MonthlyFreeQuota,
                NextTierMinSpent = nextThreshold?.MinTotalSpent,
                AmountToNextTier = isMax ? null : Math.Max(0m, nextThreshold!.MinTotalSpent - totalSpent),

                ProgressPercent = Math.Round(progress, 1),
                IsMaxTier = isMax
            };
        }
    }
}
