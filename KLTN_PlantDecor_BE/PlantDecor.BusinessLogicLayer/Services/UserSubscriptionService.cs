using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class UserSubscriptionService : IUserSubscriptionService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserSubscriptionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<UserSubscriptionResponseDto>> GetByUserIdAsync(int userId)
        {
            var subs = await _unitOfWork.UserSubscriptionRepository.GetAllByUserIdAsync(userId);

            var result = new List<UserSubscriptionResponseDto>();
            foreach (var sub in subs)
            {
                var totalQuota = sub.QuotaOverride ?? sub.TierPackage?.QuotaRequests ?? 0;
                var usedQuota = await _unitOfWork.UserSubscriptionRepository.CountUsedQuotaAsync(sub.Id);
                var remaining = Math.Max(0, totalQuota - usedQuota);

                result.Add(new UserSubscriptionResponseDto
                {
                    Id = sub.Id,
                    PackageName = sub.IsMonthlyFree ? "Monthly Free Quota" : (sub.TierPackage?.Name ?? "Unknown"),
                    TotalQuota = totalQuota,
                    UsedQuota = usedQuota,
                    RemainingQuota = remaining,
                    StartDate = sub.StartDate,
                    EndDate = sub.EndDate,
                    IsActive = sub.IsActive,
                    IsMonthlyFree = sub.IsMonthlyFree,
                    PaidAt = sub.PaidAt,
                    CreatedAt = sub.CreatedAt
                });
            }

            return result;
        }
    }
}
