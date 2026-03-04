using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface INurseryRepository : IGenericRepository<Nursery>
    {
        Task<PaginatedResult<Nursery>> GetAllWithDetailsAsync(Pagination pagination);
        Task<Nursery?> GetByIdWithDetailsAsync(int id);
        Task<Nursery?> GetByManagerIdAsync(int managerId);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<PaginatedResult<Nursery>> GetActiveNurseriesAsync(Pagination pagination);
    }
}
