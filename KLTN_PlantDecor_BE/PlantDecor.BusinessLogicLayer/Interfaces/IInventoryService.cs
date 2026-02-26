using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IInventoryService
    {
        // CRUD Operations
        Task<PaginatedResult<InventoryListResponseDto>> GetAllInventoriesAsync(Pagination pagination);
        Task<PaginatedResult<InventoryListResponseDto>> GetActiveInventoriesAsync(Pagination pagination);
        Task<InventoryResponseDto?> GetInventoryByIdAsync(int id);
        Task<InventoryResponseDto> CreateInventoryAsync(InventoryRequestDto request);
        Task<InventoryResponseDto> UpdateInventoryAsync(int id, InventoryUpdateDto request);
        Task<bool> DeleteInventoryAsync(int id);
        Task<bool> ToggleActiveAsync(int id);

        // Category & Tag Assignment
        Task<InventoryResponseDto> AssignCategoriesToInventoryAsync(AssignInventoryCategoriesDto request);
        Task<InventoryResponseDto> AssignTagsToInventoryAsync(AssignInventoryTagsDto request);
        Task<InventoryResponseDto> RemoveCategoryFromInventoryAsync(int inventoryId, int categoryId);
        Task<InventoryResponseDto> RemoveTagFromInventoryAsync(int inventoryId, int tagId);

        // Stock Management
        Task<InventoryResponseDto> UpdateStockAsync(int id, int quantity);

        // Shop Display
        Task<PaginatedResult<InventoryListResponseDto>> GetInventoriesForShopAsync(Pagination pagination);
    }
}
