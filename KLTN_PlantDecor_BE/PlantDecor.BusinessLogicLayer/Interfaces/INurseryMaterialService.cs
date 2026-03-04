using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface INurseryMaterialService
    {
        // CRUD Operations
        Task<PaginatedResult<NurseryMaterialListResponseDto>> GetAllNurseryMaterialsAsync(Pagination pagination);
        Task<NurseryMaterialResponseDto?> GetNurseryMaterialByIdAsync(int id);
        Task<NurseryMaterialResponseDto> CreateNurseryMaterialAsync(NurseryMaterialRequestDto request);
        Task<NurseryMaterialResponseDto> UpdateNurseryMaterialAsync(int id, NurseryMaterialUpdateDto request);
        Task<bool> DeleteNurseryMaterialAsync(int id);

        // Query Operations
        Task<PaginatedResult<NurseryMaterialListResponseDto>> GetByNurseryIdAsync(int nurseryId, Pagination pagination);
        Task<PaginatedResult<NurseryMaterialListResponseDto>> GetByMaterialIdAsync(int materialId, Pagination pagination);

        // Stock Management
        Task<NurseryMaterialResponseDto> ImportMaterialAsync(int nurseryId, ImportMaterialRequestDto request);
        Task<NurseryMaterialResponseDto> UpdateQuantityAsync(int nurseryId, int materialId, int quantity);

        // Manager Operations
        Task<PaginatedResult<NurseryMaterialListResponseDto>> GetMyNurseryMaterialsAsync(int managerId, Pagination pagination);
        Task<NurseryMaterialResponseDto> ImportToMyNurseryAsync(int managerId, ImportMaterialRequestDto request);
    }
}
