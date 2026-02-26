using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IInventoryRepository : IGenericRepository<Inventory>
    {
        Task<PaginatedResult<Inventory>> GetAllWithDetailsAsync(Pagination pagination);
        Task<PaginatedResult<Inventory>> GetActiveWithDetailsAsync(Pagination pagination);
        Task<Inventory?> GetByIdWithDetailsAsync(int id);
        Task<Inventory?> GetByIdWithOrdersAsync(int id);
        Task<bool> ExistsByCodeAsync(string inventoryCode, int? excludeId = null);
        Task<PaginatedResult<Inventory>> GetInventoriesForShopAsync(Pagination pagination);
    }
}
