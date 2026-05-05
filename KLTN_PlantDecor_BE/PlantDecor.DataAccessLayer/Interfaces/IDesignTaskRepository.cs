using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IDesignTaskRepository : IGenericRepository<DesignTask>
    {
        Task<DesignTask?> GetByIdWithDetailsAsync(int id);
        Task<List<DesignTask>> GetByRegistrationIdAsync(int designRegistrationId);
        Task<List<DesignTask>> GetByAssignedStaffIdAndDateRangeAsync(int assignedStaffId, DateOnly from, DateOnly to);
        Task<PaginatedResult<DesignTask>> GetByAssignedStaffIdAsync(
            int assignedStaffId,
            Pagination pagination,
            int? status = null,
            DateOnly? from = null,
            DateOnly? to = null);
    }
}