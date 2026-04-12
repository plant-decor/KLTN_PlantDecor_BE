using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IServiceRegistrationRepository : IGenericRepository<ServiceRegistration>
    {
        Task<ServiceRegistration?> GetByIdWithDetailsAsync(int id);
        Task<PaginatedResult<ServiceRegistration>> GetByUserIdAsync(int userId, Pagination pagination, int? status = null);
        Task<PaginatedResult<ServiceRegistration>> GetPendingByNurseryIdAsync(int nurseryId, Pagination pagination);
        Task<PaginatedResult<ServiceRegistration>> GetAllByNurseryIdAsync(int nurseryId, Pagination pagination, int? status = null);
        Task<ServiceRegistration?> GetByOrderIdAsync(int orderId);
        Task<PaginatedResult<ServiceRegistration>> GetByCaretakerIdAsync(int caretakerId, Pagination pagination, int? status = null);
    }
}
