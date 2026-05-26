using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ITierThresholdService
    {
        Task<List<TierThresholdResponseDto>> GetAllActiveAsync();
        Task<List<TierThresholdResponseDto>> GetAllAsync();
        Task<TierThresholdResponseDto> CreateAsync(CreateTierThresholdDto dto);
        Task<TierThresholdResponseDto> UpdateAsync(int id, UpdateTierThresholdDto dto);
        Task DeactivateAsync(int id);
    }
}
