using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IPlantInventoryRepository : IGenericRepository<PlantInventory>
    {
        Task<PaginatedResult<PlantInventory>> GetAllWithDetailsAsync(Pagination pagination);
        Task<PlantInventory?> GetByIdWithDetailsAsync(int id);
        Task<PaginatedResult<PlantInventory>> GetByPlantIdAsync(int plantId, Pagination pagination);
        Task<PaginatedResult<PlantInventory>> GetByNurseryIdAsync(int nurseryId, Pagination pagination);
        Task<PlantInventory?> GetByPlantAndNurseryAsync(int plantId, int nurseryId);
        Task<bool> ExistsAsync(int plantId, int nurseryId, int? excludeId = null);
    }
}
