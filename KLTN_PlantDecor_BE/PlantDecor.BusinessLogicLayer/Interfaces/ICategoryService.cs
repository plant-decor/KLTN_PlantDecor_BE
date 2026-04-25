using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ICategoryService
    {
        Task<PaginatedResult<CategoryResponseDto>> GetAllCategoriesAsync(Pagination pagination);
        Task<List<CategoryResponseDto>> GetAllActiveCategoriesAsync();
        Task<List<CategoryResponseDto>> GetCategoriesByTypeAsync(int categoryType, bool activeOnly = true);
        Task<List<CategoryResponseDto>> GetRootCategoriesAsync();
        Task<List<CategoryResponseDto>> GetRootActiveCategoriesAsync();
        Task<CategoryResponseDto> GetCategoryByIdAsync(int id);
        Task<CategoryResponseDto> CreateCategoryAsync(CategoryRequestDto request);
        Task<CategoryResponseDto> UpdateCategoryAsync(int id, CategoryUpdateDto request);
        Task<bool> DeleteCategoryAsync(int id);
        Task<bool> ToggleActiveAsync(int id);
    }
}
