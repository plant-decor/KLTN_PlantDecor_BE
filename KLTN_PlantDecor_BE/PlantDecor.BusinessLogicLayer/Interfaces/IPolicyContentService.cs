using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IPolicyContentService
    {
        Task<List<PolicyContentResponseDto>> GetAllActiveAsync();
        Task<List<PolicyContentResponseDto>> GetByCategoryActiveAsync(int category);
        Task<PolicyContentResponseDto> GetByIdAsync(int id, bool includeInactive = false);

        Task<List<PolicyContentResponseDto>> GetAdminListAsync(bool includeInactive = true);
        Task<PolicyContentResponseDto> CreateAsync(CreatePolicyContentRequestDto request);
        Task<PolicyContentResponseDto> UpdateAsync(int id, UpdatePolicyContentRequestDto request);
        Task<PolicyContentResponseDto> SetActiveStatusAsync(int id, bool isActive);
        Task DeleteAsync(int id);
    }
}
