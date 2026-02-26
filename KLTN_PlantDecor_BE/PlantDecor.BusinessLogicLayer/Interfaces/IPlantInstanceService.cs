using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IPlantInstanceService
    {
        Task<PaginatedResult<PlantInstanceResponseDto>> GetAllInstancesAsync(Pagination pagination);
        Task<PaginatedResult<PlantInstanceResponseDto>> GetInstancesByPlantIdAsync(int plantId, Pagination pagination);
        Task<PlantInstanceResponseDto?> GetInstanceByIdAsync(int id);
        Task<PlantInstanceResponseDto> CreateInstanceAsync(PlantInstanceRequestDto request);
        Task<PlantInstanceResponseDto> UpdateInstanceAsync(int id, PlantInstanceUpdateDto request);
        Task<bool> DeleteInstanceAsync(int id);
        Task<bool> UpdateStatusAsync(int id, int status);
    }
}
