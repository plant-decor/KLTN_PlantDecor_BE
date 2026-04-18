using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IDesignTemplateTierService
    {
        Task<List<DesignTemplateTierResponseDto>> GetByTemplateIdAsync(int designTemplateId, bool includeInactive = false);
        Task<DesignTemplateTierResponseDto> GetByIdAsync(int id);
        Task<DesignTemplateTierResponseDto> CreateAsync(CreateDesignTemplateTierRequestDto request);
        Task<DesignTemplateTierResponseDto> UpdateAsync(int id, UpdateDesignTemplateTierRequestDto request);
        Task<DesignTemplateTierResponseDto> SetItemsAsync(int id, List<DesignTemplateTierItemInputDto> items);
        Task DeleteAsync(int id);
    }
}