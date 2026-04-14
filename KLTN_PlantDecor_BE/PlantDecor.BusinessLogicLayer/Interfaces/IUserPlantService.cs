using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IUserPlantService
    {
        Task<List<UserPlantResponseDto>> GetMyPlantsAsync(int userId);
    }
}