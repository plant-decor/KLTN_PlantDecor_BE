using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IUserPlantRepository : IGenericRepository<UserPlant>
    {
        Task<List<UserPlant>> GetByUserIdWithDetailsAsync(int userId);
        Task<bool> ExistsByUserIdAndPlantInstanceIdAsync(int userId, int plantInstanceId);
        Task<bool> ExistsByUserIdAndPlantIdAsync(int userId, int plantId);
    }
}