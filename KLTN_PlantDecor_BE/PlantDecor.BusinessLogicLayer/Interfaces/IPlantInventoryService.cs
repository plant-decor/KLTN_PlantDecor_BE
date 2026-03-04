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
        Task<CommonPlantResponseDto?> GetCommonPlantByIdAsync(int id);
        Task<CommonPlantResponseDto> CreateCommonPlantAsync(CommonPlantRequestDto request);
        Task<CommonPlantResponseDto> UpdateCommonPlantAsync(int id, CommonPlantUpdateDto request);
        Task<bool> DeleteCommonPlantAsync(int id);

        // Query Operations
        Task<PaginatedResult<CommonPlantListResponseDto>> GetByPlantIdAsync(int plantId, Pagination pagination);
        Task<PaginatedResult<CommonPlantListResponseDto>> GetByNurseryIdAsync(int nurseryId, Pagination pagination);

        // Stock Management
        Task<CommonPlantResponseDto> UpdateQuantityAsync(int nurseryId, int plantId, int quantity);
    }
}
