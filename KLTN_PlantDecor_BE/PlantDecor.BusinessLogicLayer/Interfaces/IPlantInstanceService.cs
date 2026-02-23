using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IPlantInstanceService
    {
        Task<List<PlantInstanceResponseDto>> GetAllInstancesAsync();
        Task<List<PlantInstanceResponseDto>> GetInstancesByPlantIdAsync(int plantId);
        Task<PlantInstanceResponseDto?> GetInstanceByIdAsync(int id);
        Task<PlantInstanceResponseDto> CreateInstanceAsync(PlantInstanceRequestDto request);
        Task<PlantInstanceResponseDto> UpdateInstanceAsync(int id, PlantInstanceUpdateDto request);
        Task<bool> DeleteInstanceAsync(int id);
        Task<bool> UpdateStatusAsync(int id, int status);
    }
}
