using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IAIQuotaService
    {
        Task<UserAIUsage> ConsumeAsync(int userId, AIEndpointTypeEnum endpoint, int? orderId = null, string? referenceId = null);
        Task RefundAsync(int usageId);
        Task<UserQuotaStatusDto> GetUserQuotaStatusAsync(int userId);
    }
}
