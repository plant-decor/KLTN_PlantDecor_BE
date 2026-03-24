using Microsoft.AspNetCore.Http;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IPlantService
    {
        // CRUD Operations
        Task<PaginatedResult<PlantListResponseDto>> GetAllPlantsAsync(Pagination pagination);
        Task<PaginatedResult<PlantListResponseDto>> GetActivePlantsAsync(Pagination pagination);
        Task<PaginatedResult<PlantListResponseDto>> SearchAllPlantsAsync(PlantSearchRequestDto request);
        Task<PaginatedResult<PlantListResponseDto>> SearchPlantsForShopAsync(PlantSearchRequestDto request);
        Task<PlantResponseDto?> GetPlantByIdAsync(int id);
        Task<PlantResponseDto> CreatePlantAsync(PlantRequestDto request);
        Task<PlantResponseDto> UpdatePlantAsync(int id, PlantUpdateDto request);
        Task<PlantResponseDto> UploadPlantImagesAsync(int plantId, List<IFormFile> files);
        Task<bool> DeletePlantAsync(int id);
        Task<bool> ToggleActiveAsync(int id);

        // Category & Tag Assignment
        Task<PlantResponseDto> AssignCategoriesToPlantAsync(AssignCategoriesDto request);
        Task<PlantResponseDto> AssignTagsToPlantAsync(AssignTagsDto request);
        Task<PlantResponseDto> RemoveCategoryFromPlantAsync(int plantId, int categoryId);
        Task<PlantResponseDto> RemoveTagFromPlantAsync(int plantId, int tagId);

        // Shop Display (with available instances)
        Task<PaginatedResult<PlantListResponseDto>> GetPlantsForShopAsync(Pagination pagination);
    }
}
