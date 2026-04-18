using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ICommonPlantService
    {
        // CRUD Operations
        Task<PaginatedResult<CommonPlantListResponseDto>> GetAllCommonPlantsAsync(Pagination pagination);
        Task<CommonPlantResponseDto> GetCommonPlantByIdAsync(int id);
        Task<CommonPlantResponseDto> CreateCommonPlantAsync(int nurseryId, int managerId, CommonPlantRequestDto request);
        Task<CommonPlantResponseDto> UpdateCommonPlantAsync(int id, CommonPlantUpdateDto request);
        Task<bool> DeleteCommonPlantAsync(int id);

        // Query Operations
        Task<PaginatedResult<CommonPlantListResponseDto>> GetByPlantIdAsync(int plantId, Pagination pagination);
        Task<PaginatedResult<CommonPlantListResponseDto>> GetByNurseryIdAsync(int nurseryId, Pagination pagination);

        // Stock Management
        Task<CommonPlantResponseDto> UpdateQuantityAsync(int nurseryId, int plantId, int quantity);

        // Manager Operations
        Task<CommonPlantResponseDto> CreateForNurseryAsync(int nurseryId, int managerId, CommonPlantRequestDto request);
        Task<PaginatedResult<CommonPlantListResponseDto>> GetByNurseryForManagerAsync(int nurseryId, int managerId, Pagination pagination);
        Task<PaginatedResult<PlantListResponseDto>> GetPlantsNotInNurseryForManagerAsync(int nurseryId, int managerId, Pagination pagination);
        Task<CommonPlantResponseDto> UpdateForManagerAsync(int nurseryId, int commonPlantId, int managerId, CommonPlantUpdateDto request);
        Task<CommonPlantResponseDto> ToggleActiveAsync(int nurseryId, int commonPlantId, int managerId);

        // Shop Operations
        Task<PaginatedResult<CommonPlantListResponseDto>> GetActiveByNurseryIdAsync(int nurseryId, Pagination pagination);
        Task<List<PlantNurseryAvailabilityDto>> GetNurseriesWithCommonPlantAsync(int plantId);
        Task<PaginatedResult<CommonPlantListResponseDto>> SearchCommonPlantsForShopAsync(CommonPlantShopSearchRequestDto searchRequest, Pagination pagination);
    }
}
