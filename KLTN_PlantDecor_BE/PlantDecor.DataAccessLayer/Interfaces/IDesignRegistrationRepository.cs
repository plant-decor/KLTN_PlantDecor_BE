using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IDesignRegistrationRepository : IGenericRepository<DesignRegistration>
    {
        Task<DesignRegistration?> GetByIdWithDetailsAsync(int id);
        Task<PaginatedResult<DesignRegistration>> GetByUserIdAsync(int userId, Pagination pagination, int? status = null);
        Task<PaginatedResult<DesignRegistration>> GetByNurseryIdAsync(int nurseryId, Pagination pagination, int? status = null);
        Task<DesignRegistration?> GetByOrderIdAsync(int orderId);
    }
}