using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IPlantRepository : IGenericRepository<Plant>
    {
        Task<List<Plant>> GetAllWithDetailsAsync();
        Task<List<Plant>> GetActiveWithDetailsAsync();
        Task<Plant?> GetByIdWithDetailsAsync(int id);
        Task<Plant?> GetByIdWithInstancesAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<List<Plant>> GetPlantsForShopAsync();
    }
}
