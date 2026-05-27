using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ITierService
    {
        Task RecalculateTierAsync(int userId);
        Task<TierProgressDto> GetTierProgressAsync(int userId);
    }
}
