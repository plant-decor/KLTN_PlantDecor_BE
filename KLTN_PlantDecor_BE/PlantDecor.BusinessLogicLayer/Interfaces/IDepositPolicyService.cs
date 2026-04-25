using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IDepositPolicyService
    {
        Task<List<DepositPolicyResponseDto>> GetAllAsync();
        Task<DepositPolicyResponseDto> GetByIdAsync(int id);
        Task<DepositPolicyResponseDto> CreateAsync(DepositPolicyRequestDto request);
        Task<DepositPolicyResponseDto> UpdateAsync(int id, UpdateDepositPolicyRequestDto request);
        Task DeleteAsync(int id);
    }
}
