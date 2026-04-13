using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ILayoutDesignImageGenerationService
    {
        Task<LayoutDesignImageGenerationResultDto> GenerateImagesAsync(int layoutDesignId, int userId);
        Task<List<LayoutDesignGeneratedImageDto>> GetGeneratedImagesAsync(int layoutDesignId, int userId);
    }
}
