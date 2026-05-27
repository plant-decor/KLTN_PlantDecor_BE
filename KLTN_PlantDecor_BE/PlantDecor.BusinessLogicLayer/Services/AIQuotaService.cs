using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class AIQuotaService : IAIQuotaService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<AIQuotaService> _logger;

        public AIQuotaService(IUnitOfWork unitOfWork, ILogger<AIQuotaService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<UserAIUsage> ConsumeAsync(
            int userId,
            AIEndpointTypeEnum endpoint,
            int? orderId = null,
            string? referenceId = null)
        {
            // FIFO: earliest-expiring subscription first
            // Lấy danh sách gói đang active Sắp xếp theo EndDate tăng dần → gói sắp hết hạn lên đầu (FIFO)
            var activeSubs = await _unitOfWork.UserSubscriptionRepository.GetActiveByUserIdAsync(userId);

            UserSubscription? targetSub = null;
            // Tìm gói còn quota để sử dụng (đếm số đã dùng so với quota của gói)
            foreach (var sub in activeSubs)
            {
                var used = await _unitOfWork.UserSubscriptionRepository.CountUsedQuotaAsync(sub.Id); // đã dùng bao nhiêu
                var capacity = sub.QuotaOverride ?? sub.TierPackage?.QuotaRequests ?? 0; // Tổng được phép dùng của gói
                if (used < capacity)
                {
                    targetSub = sub;
                    break;
                }
            }

            if (targetSub == null)
            {
                throw new ForbiddenException("AI quota exhausted. Please purchase a package to continue.");
            }

            var usage = new UserAIUsage
            {
                UserId = userId,
                UserSubscriptionId = targetSub.Id,
                EndpointType = (int)endpoint,
                RequestCount = 1,
                OrderId = orderId,
                ReferenceId = referenceId,
                IsSuccess = true,
                IsRefunded = false,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.UserAIUsageRepository.CreateAsync(usage);

            _logger.LogInformation(
                "AIQuotaService: Consumed quota for User={UserId} Endpoint={Endpoint} Sub={SubId} Ref={Ref}",
                userId, endpoint, targetSub.Id, referenceId);

            return usage;
        }

        public async Task RefundAsync(int usageId)
        {
            var usage = await _unitOfWork.UserAIUsageRepository.GetByIdAsync(usageId);
            if (usage == null || usage.IsRefunded) return;

            usage.IsSuccess = false;
            usage.IsRefunded = true;
            _unitOfWork.UserAIUsageRepository.PrepareUpdate(usage);
            await _unitOfWork.SaveAsync();

            _logger.LogInformation("AIQuotaService: Refunded usage {UsageId}", usageId);
        }

        public async Task<UserQuotaStatusDto> GetUserQuotaStatusAsync(int userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User not found");

            var activeSubs = await _unitOfWork.UserSubscriptionRepository.GetActiveByUserIdAsync(userId);
            var threshold = await _unitOfWork.TierThresholdRepository.GetByTierLevelAsync(user.TierLevel);

            var subscriptionDtos = new List<SubscriptionQuotaDto>();
            var totalRemaining = 0;

            foreach (var sub in activeSubs)
            {
                var totalQuota = sub.QuotaOverride ?? sub.TierPackage?.QuotaRequests ?? 0;
                var usedQuota = await _unitOfWork.UserSubscriptionRepository.CountUsedQuotaAsync(sub.Id);
                var remaining = Math.Max(0, totalQuota - usedQuota);
                totalRemaining += remaining;

                subscriptionDtos.Add(new SubscriptionQuotaDto
                {
                    SubscriptionId = sub.Id,
                    PackageName = sub.IsMonthlyFree ? "Monthly Free Quota" : (sub.TierPackage?.Name ?? "Unknown"),
                    TotalQuota = totalQuota,
                    UsedQuota = usedQuota,
                    RemainingQuota = remaining,
                    EndDate = sub.EndDate,
                    IsMonthlyFree = sub.IsMonthlyFree
                });
            }

            return new UserQuotaStatusDto
            {
                TierLevel = user.TierLevel,
                TierName = threshold?.Name,
                TierMonthlyFreeQuota = threshold?.MonthlyFreeQuota ?? 0,
                TotalRemainingQuota = totalRemaining,
                ActiveSubscriptions = subscriptionDtos
            };
        }
    }
}
