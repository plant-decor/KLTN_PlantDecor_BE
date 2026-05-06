using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IDesignTemplateService
    {
        Task<List<DesignTemplateResponseDto>> GetAllAdminAsync();
        Task<List<DesignTemplateResponseDto>> GetAllAsync();
        Task<DesignTemplateResponseDto> GetByIdAsync(int id);
        Task<DesignTemplateResponseDto> GetByIdAdminAsync(int id);
        Task<DesignTemplateResponseDto> CreateAsync(CreateDesignTemplateRequestDto request);
        Task<DesignTemplateResponseDto> UpdateAsync(int id, UpdateDesignTemplateRequestDto request);
        Task<DesignTemplateResponseDto> UpdateSpecializationsAsync(int templateId, List<int> specializationIds);
        Task DeleteAsync(int id);
    }
}