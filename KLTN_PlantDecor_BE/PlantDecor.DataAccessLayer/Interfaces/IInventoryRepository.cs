using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IInventoryRepository : IGenericRepository<Inventory>
    {
        Task<List<Inventory>> GetAllWithDetailsAsync();
        Task<List<Inventory>> GetActiveWithDetailsAsync();
        Task<Inventory?> GetByIdWithDetailsAsync(int id);
        Task<Inventory?> GetByIdWithOrdersAsync(int id);
        Task<bool> ExistsByCodeAsync(string inventoryCode, int? excludeId = null);
        Task<List<Inventory>> GetInventoriesForShopAsync();
    }
}
