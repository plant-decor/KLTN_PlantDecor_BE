using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IPlantRepository : IGenericRepository<Plant>
    {
        Task<PaginatedResult<Plant>> GetAllWithDetailsAsync(Pagination pagination);
        Task<PaginatedResult<Plant>> GetActiveWithDetailsAsync(Pagination pagination);
        Task<PaginatedResult<Plant>> GetActivePlantsNotInNurseryAsync(int nurseryId, Pagination pagination);
        Task<Plant?> GetByIdWithDetailsAsync(int id);
        Task<Plant?> GetByIdWithInstancesAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<bool> ExistsByIdAsync(int id);
        Task<PaginatedResult<Plant>> GetPlantsForShopAsync(Pagination pagination);
        Task<PaginatedResult<Plant>> SearchAllWithDetailsAsync(PlantSearchFilter filter, Pagination pagination);
        Task<PaginatedResult<Plant>> SearchForShopAsync(PlantSearchFilter filter, Pagination pagination);
    }
}
