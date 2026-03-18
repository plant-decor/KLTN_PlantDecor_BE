using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class OrderRepository : GenericRepository<Order>, IOrderRepository
    {
        public OrderRepository(PlantDecorContext context) : base(context) { }

        public async Task<Order?> GetByIdWithDetailsAsync(int orderId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                .Include(o => o.Invoices)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<List<Order>> GetByGroupCodeWithDetailsAsync(string orderGroupCode)
        {
            // Temporarily disabled because OrderGroupCode is not used in current flow.
            return await Task.FromResult(new List<Order>());
        }
    }
}
