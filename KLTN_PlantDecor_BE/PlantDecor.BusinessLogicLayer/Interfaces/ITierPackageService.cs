using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ITierPackageService
    {
        Task<List<TierPackageResponseDto>> GetAllActiveAsync();
        Task<List<TierPackageResponseDto>> GetAllAsync();
        Task<TierPackageResponseDto> GetByIdAsync(int id);
        Task<TierPackageResponseDto> CreateAsync(CreateTierPackageDto dto);
        Task<TierPackageResponseDto> UpdateAsync(int id, UpdateTierPackageDto dto);
        Task DeactivateAsync(int id);
    }
}
