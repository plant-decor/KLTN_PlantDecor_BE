using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class ReturnTicketRepository : GenericRepository<ReturnTicket>, IReturnTicketRepository
    {
        public ReturnTicketRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<List<ReturnTicket>> GetByCustomerIdWithDetailsAsync(int customerId)
        {
            return await _context.ReturnTickets
                .Include(rt => rt.ReturnTicketItems)
                    .ThenInclude(i => i.NurseryOrderDetail)
                        .ThenInclude(d => d.NurseryOrder)
                .Include(rt => rt.ReturnTicketItems)
                    .ThenInclude(i => i.ReturnTicketItemImages)
                .Include(rt => rt.ReturnTicketAssignments)
                    .ThenInclude(a => a.Manager)
                .Where(rt => rt.CustomerId == customerId)
                .OrderByDescending(rt => rt.CreatedAt)
                .ToListAsync();
        }
    }
}
