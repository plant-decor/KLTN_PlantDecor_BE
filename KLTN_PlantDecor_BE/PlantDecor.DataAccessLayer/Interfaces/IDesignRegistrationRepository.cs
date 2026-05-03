using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IDesignRegistrationRepository : IGenericRepository<DesignRegistration>
    {
        Task<DesignRegistration?> GetByIdWithDetailsAsync(int id);
        Task<PaginatedResult<DesignRegistration>> GetByUserIdAsync(int userId, Pagination pagination, int? status = null);
        Task<PaginatedResult<DesignRegistration>> GetPendingByNurseryIdAsync(int nurseryId, Pagination pagination);
        Task<PaginatedResult<DesignRegistration>> GetByNurseryIdAsync(int nurseryId, Pagination pagination, int? status = null);
        Task<DesignRegistration?> GetByOrderIdAsync(int orderId);
        Task<PaginatedResult<DesignRegistration>> GetByAssignedCaretakerIdAsync(int caretakerId, Pagination pagination, int? status = null);
        Task<PaginatedResult<DesignRegistration>> GetByAssignedCaretakerIdWithStatusesAsync(int caretakerId, List<int> statuses, Pagination pagination);
        Task<Dictionary<int, int>> CountOpenByNurseryIdsAsync(List<int> nurseryIds);
        Task<Dictionary<int, int>> CountOpenAssignmentsByCaretakerIdsAsync(List<int> caretakerIds, List<int> nurseryIds);
    }
}
