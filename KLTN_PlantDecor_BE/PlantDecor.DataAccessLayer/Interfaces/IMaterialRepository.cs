using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IMaterialRepository : IGenericRepository<Material>
    {
        Task<PaginatedResult<Material>> GetAllWithDetailsAsync(Pagination pagination, string? keyword = null);
        Task<PaginatedResult<Material>> GetActiveWithDetailsAsync(Pagination pagination, string? keyword = null);
        Task<Material?> GetByIdWithDetailsAsync(int id);
        Task<Material?> GetByIdWithOrdersAsync(int id);
        Task<bool> ExistsByCodeAsync(string materialCode, int? excludeId = null);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<PaginatedResult<Material>> GetMaterialsForShopAsync(Pagination pagination);
    }
}
