using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class UserPlantService : IUserPlantService
    {
        private readonly IUnitOfWork _unitOfWork;

        public UserPlantService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<List<UserPlantResponseDto>> GetMyPlantsAsync(int userId)
        {
            var userPlants = await _unitOfWork.UserPlantRepository.GetByUserIdWithDetailsAsync(userId);
            return userPlants.ToResponseList();
        }
    }
}