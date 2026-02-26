using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IPlantInventoryService
    {
        // CRUD Operations
        Task<PaginatedResult<PlantInventoryListResponseDto>> GetAllPlantInventoriesAsync(Pagination pagination);
        Task<PlantInventoryResponseDto?> GetPlantInventoryByIdAsync(int id);
        Task<PlantInventoryResponseDto> CreatePlantInventoryAsync(PlantInventoryRequestDto request);
        Task<PlantInventoryResponseDto> UpdatePlantInventoryAsync(int id, PlantInventoryUpdateDto request);
        Task<bool> DeletePlantInventoryAsync(int id);

        // Query Operations
        Task<PaginatedResult<PlantInventoryListResponseDto>> GetByPlantIdAsync(int plantId, Pagination pagination);
        Task<PaginatedResult<PlantInventoryListResponseDto>> GetByNurseryIdAsync(int nurseryId, Pagination pagination);

        // Stock Management
        Task<PlantInventoryResponseDto> UpdateQuantityAsync(int nurseryId, int plantId, int quantity);
    }
}
