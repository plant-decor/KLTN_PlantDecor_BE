using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IPlantService
    {
        // CRUD Operations
        Task<List<PlantListResponseDto>> GetAllPlantsAsync();
        Task<List<PlantListResponseDto>> GetActivePlantsAsync();
        Task<PlantResponseDto?> GetPlantByIdAsync(int id);
        Task<PlantResponseDto> CreatePlantAsync(PlantRequestDto request);
        Task<PlantResponseDto> UpdatePlantAsync(int id, PlantUpdateDto request);
        Task<bool> DeletePlantAsync(int id);
        Task<bool> ToggleActiveAsync(int id);

        // Category & Tag Assignment
        Task<PlantResponseDto> AssignCategoriesToPlantAsync(AssignCategoriesDto request);
        Task<PlantResponseDto> AssignTagsToPlantAsync(AssignTagsDto request);
        Task<PlantResponseDto> RemoveCategoryFromPlantAsync(int plantId, int categoryId);
        Task<PlantResponseDto> RemoveTagFromPlantAsync(int plantId, int tagId);

        // Shop Display (with available instances)
        Task<List<PlantListResponseDto>> GetPlantsForShopAsync();
    }
}
