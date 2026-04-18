using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class ReturnTicketAssignmentRepository : GenericRepository<ReturnTicketAssignment>, IReturnTicketAssignmentRepository
    {
        public ReturnTicketAssignmentRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<List<ReturnTicketAssignment>> GetByManagerIdWithDetailsAsync(int managerId)
        {
            return await BuildDetailedQuery()
                .Where(a => a.ManagerId == managerId)
                .OrderByDescending(a => a.UpdatedAt ?? a.AssignedAt)
                .ToListAsync();
        }

        public async Task<ReturnTicketAssignment?> GetByIdWithDetailsAsync(int assignmentId)
        {
            return await BuildDetailedQuery()
                .FirstOrDefaultAsync(a => a.Id == assignmentId);
        }

        private IQueryable<ReturnTicketAssignment> BuildDetailedQuery()
        {
            return _context.ReturnTicketAssignments
                .Include(a => a.Nursery)
                .Include(a => a.Manager)
                .Include(a => a.ReturnTicket)
                    .ThenInclude(rt => rt.Customer)
                .Include(a => a.ReturnTicket)
                    .ThenInclude(rt => rt.ReturnTicketItems)
                        .ThenInclude(i => i.NurseryOrderDetail)
                            .ThenInclude(d => d.NurseryOrder)
                .Include(a => a.ReturnTicket)
                    .ThenInclude(rt => rt.ReturnTicketItems)
                        .ThenInclude(i => i.ReturnTicketItemImages)
                .Include(a => a.ReturnTicket)
                    .ThenInclude(rt => rt.ReturnTicketAssignments)
                .Include(a => a.ReturnTicket)
                    .ThenInclude(rt => rt.Order)
                        .ThenInclude(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails);
        }
    }
}
