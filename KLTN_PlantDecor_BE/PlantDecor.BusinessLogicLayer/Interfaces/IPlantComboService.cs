using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IPlantComboService
    {
        // CRUD Operations
        Task<PaginatedResult<PlantComboListResponseDto>> GetAllCombosAsync(Pagination pagination);
        Task<PaginatedResult<PlantComboListResponseDto>> GetActiveCombosAsync(Pagination pagination);
        Task<PlantComboResponseDto?> GetComboByIdAsync(int id);
        Task<PlantComboResponseDto> CreateComboAsync(PlantComboRequestDto request);
        Task<PlantComboResponseDto> UpdateComboAsync(int id, PlantComboUpdateDto request);
        Task<bool> DeleteComboAsync(int id);
        Task<bool> ToggleActiveAsync(int id);

        // Combo Items Management
        Task<PlantComboResponseDto> AddComboItemAsync(int comboId, PlantComboItemRequestDto request);
        Task<PlantComboResponseDto> RemoveComboItemAsync(int comboId, int comboItemId);
        Task<PlantComboResponseDto> UpdateComboItemAsync(int comboItemId, PlantComboItemRequestDto request);

        // Tag Assignment
        Task<PlantComboResponseDto> AssignTagsToComboAsync(AssignComboTagsDto request);
        Task<PlantComboResponseDto> RemoveTagFromComboAsync(int comboId, int tagId);

        // Manager - Nursery Combo Stock
        Task<NurseryComboStockOperationResponseDto> AssembleComboStockAsync(int nurseryId, int managerId, int comboId, AssembleNurseryComboRequestDto request);
        Task<NurseryComboStockOperationResponseDto> DecomposeComboStockAsync(int nurseryId, int managerId, int comboId, DecomposeNurseryComboRequestDto request);

        // Shop Display
        Task<PaginatedResult<PlantComboListResponseDto>> GetCombosForShopAsync(Pagination pagination);
        Task<PaginatedResult<SellingPlantComboResponseDto>> GetSellingCombosAsync(Pagination pagination, PlantComboShopSearchRequestDto searchDto);
    }
}
