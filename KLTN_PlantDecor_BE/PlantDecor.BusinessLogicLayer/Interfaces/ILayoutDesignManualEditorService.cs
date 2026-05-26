using Microsoft.AspNetCore.Http;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ILayoutDesignManualEditorService
    {
        Task<LayoutDesignManualEditorContextDto> GetEditorContextAsync(int layoutDesignId, int userId);
        Task<LayoutDesignManualEditorImageDto> SaveCompositeDraftAsync(int layoutDesignId, int userId, string layerJson);
        Task<LayoutDesignManualEditorImageDto> SavePlantDraftAsync(int layoutDesignId, int layoutDesignPlantId, int userId, string layerJson);
        Task<LayoutDesignManualEditorImageDto> PublishCompositeAsync(int layoutDesignId, int userId, IFormFile imageFile, string? layerJson);
        Task<LayoutDesignManualEditorImageDto> PublishPlantAsync(int layoutDesignId, int layoutDesignPlantId, int userId, IFormFile imageFile, string? layerJson);
        Task<LayoutDesignManualEditorImageDto> BeautifyCompositeAsync(int layoutDesignId, int userId, IFormFile? imageFile, string? imageUrl, string? layerJson);
        Task<LayoutDesignManualCalculateResponseDto> CalculateManualTotalAsync(int layoutDesignId, int userId, LayoutDesignManualCalculateRequestDto request);
    }
}
