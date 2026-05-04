using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using Microsoft.AspNetCore.Http;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IMaterialService
    {
        // CRUD Operations
        Task<PaginatedResult<MaterialListResponseDto>> GetAllMaterialsAsync(Pagination pagination, string? keyword = null);
        Task<PaginatedResult<MaterialListResponseDto>> GetActiveMaterialsAsync(Pagination pagination, string? keyword = null);
        Task<MaterialResponseDto> GetMaterialByIdAsync(int id);
        Task<MaterialResponseDto> CreateMaterialAsync(MaterialRequestDto request);
        Task<MaterialResponseDto> UpdateMaterialAsync(int id, MaterialUpdateDto request);
        Task<MaterialResponseDto> UploadMaterialThumbnailAsync(int materialId, IFormFile file);
        Task<MaterialResponseDto> UploadMaterialImagesAsync(int materialId, List<IFormFile> files);
        Task<MaterialResponseDto> SetPrimaryMaterialImageAsync(int materialId, int imageId);
        Task<MaterialResponseDto> ReplaceImageAsync(int materialId, int imageId, IFormFile file);
        Task<MaterialResponseDto> DeleteMaterialImageAsync(int materialId, int imageId);
        Task<bool> DeleteMaterialAsync(int id);
        Task<bool> ToggleActiveAsync(int id);

        // Category & Tag Assignment
        Task<MaterialResponseDto> AssignCategoriesToMaterialAsync(AssignMaterialCategoriesDto request);
        Task<MaterialResponseDto> AssignTagsToMaterialAsync(AssignMaterialTagsDto request);
        Task<MaterialResponseDto> RemoveCategoryFromMaterialAsync(int materialId, int categoryId);
        Task<MaterialResponseDto> RemoveTagFromMaterialAsync(int materialId, int tagId);

        // Shop Display
        Task<PaginatedResult<MaterialListResponseDto>> GetMaterialsForShopAsync(Pagination pagination);
        Task<List<NurseryListResponseDto>> GetNurseriesByMaterialAsync(int materialId);
    }
}
