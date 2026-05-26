using Microsoft.Extensions.Logging;
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
    }
}
