using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IUserSubscriptionService
    {
        Task<List<UserSubscriptionResponseDto>> GetByUserIdAsync(int userId);
    }
}
