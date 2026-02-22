using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IInventoryService
    {
        // CRUD Operations
        Task<List<InventoryListResponseDto>> GetAllInventoriesAsync();
        Task<List<InventoryListResponseDto>> GetActiveInventoriesAsync();
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
        Task<List<InventoryListResponseDto>> GetInventoriesForShopAsync();
    }
}
