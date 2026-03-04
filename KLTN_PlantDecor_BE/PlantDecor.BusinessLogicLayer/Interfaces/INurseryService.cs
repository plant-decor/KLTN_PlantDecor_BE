using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface INurseryService
    {
        // CRUD Operations
        Task<PaginatedResult<NurseryListResponseDto>> GetAllNurseriesAsync(Pagination pagination);
        Task<PaginatedResult<NurseryListResponseDto>> GetActiveNurseriesAsync(Pagination pagination);
        Task<NurseryResponseDto?> GetNurseryByIdAsync(int id);
        Task<NurseryResponseDto?> GetMyNurseryAsync(int managerId);
        Task<NurseryResponseDto> CreateNurseryAsync(int managerId, NurseryRequestDto request);
        Task<NurseryResponseDto> UpdateNurseryAsync(int id, NurseryUpdateDto request);
        Task<bool> DeleteNurseryAsync(int id);

        // Manager Operations
        Task<NurseryResponseDto> UpdateMyNurseryAsync(int managerId, NurseryUpdateDto request);
    }
}
