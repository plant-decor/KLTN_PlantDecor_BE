using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IReturnTicketAssignmentRepository : IGenericRepository<ReturnTicketAssignment>
    {
        Task<List<ReturnTicketAssignment>> GetByManagerIdWithDetailsAsync(int managerId);
        Task<ReturnTicketAssignment?> GetByIdWithDetailsAsync(int assignmentId);
    }
}
