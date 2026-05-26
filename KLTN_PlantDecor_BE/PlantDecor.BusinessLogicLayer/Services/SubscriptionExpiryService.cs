using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class SubscriptionExpiryService : ISubscriptionExpiryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<SubscriptionExpiryService> _logger;

        public SubscriptionExpiryService(IUnitOfWork unitOfWork, ILogger<SubscriptionExpiryService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task DeactivateSubscriptionAsync(int subscriptionId)
        {
            var sub = await _unitOfWork.UserSubscriptionRepository.GetByIdAsync(subscriptionId);
            if (sub == null || !sub.IsActive) return;

            sub.IsActive = false;
            _unitOfWork.UserSubscriptionRepository.PrepareUpdate(sub);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("SubscriptionExpiryService: Deactivated subscription {SubId}", subscriptionId);
        }
    }
}
