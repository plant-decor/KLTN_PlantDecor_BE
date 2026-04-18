using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface INurseryDesignTemplateService
    {
        Task<List<NurseryDesignTemplateResponseDto>> GetActiveByNurseryIdAsync(int nurseryId);
        Task<List<NurseryDesignTemplateResponseDto>> GetActiveByTemplateIdAsync(int designTemplateId);
        Task<List<NurseryDesignTemplateResponseDto>> GetByManagerAsync(int managerId, bool activeOnly = false);
        Task<List<DesignTemplateOptionResponseDto>> GetNotOfferedByManagerAsync(int managerId);
        Task<NurseryDesignTemplateResponseDto> AddToMyNurseryAsync(int managerId, CreateNurseryDesignTemplateRequestDto request);
        Task<NurseryDesignTemplateResponseDto> ToggleActiveAsync(int managerId, int nurseryDesignTemplateId);
        Task RemoveFromMyNurseryAsync(int managerId, int nurseryDesignTemplateId);
    }
}