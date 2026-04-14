using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IPlantGuideService
    {
        Task<PaginatedResult<PlantGuideResponseDto>> GetAllPlantGuidesAsync(Pagination pagination);
        Task<PlantGuideResponseDto?> GetPlantGuideByPlantIdAsync(int plantId);
        Task<PlantGuideResponseDto?> GetPlantGuideByIdAsync(int id);
        Task<PlantGuideResponseDto> CreatePlantGuideAsync(PlantGuideRequestDto request);
        Task<PlantGuideResponseDto> UpdatePlantGuideAsync(int id, PlantGuideUpdateDto request);
        Task<bool> DeletePlantGuideAsync(int id);
    }
}
