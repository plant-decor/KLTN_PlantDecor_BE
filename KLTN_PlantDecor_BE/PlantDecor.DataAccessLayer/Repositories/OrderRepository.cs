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
            return await BuildDetailedQuery()
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        public async Task<List<Order>> GetByUserIdWithDetailsAsync(int userId, int? orderStatus = null)
        {
            var query = BuildDetailedQuery()
                .Where(o => o.UserId == userId);

            if (orderStatus.HasValue)
                query = query.Where(o => o.Status == orderStatus.Value);

            return await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        private IQueryable<Order> BuildDetailedQuery()
        {
            return _context.Orders
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                        .ThenInclude(d => d.CommonPlant)
                            .ThenInclude(cp => cp!.Plant)
                                .ThenInclude(p => p!.PlantImages)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                        .ThenInclude(d => d.PlantInstance)
                            .ThenInclude(pi => pi!.PlantImages)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                        .ThenInclude(d => d.PlantInstance)
                            .ThenInclude(pi => pi!.Plant)
                                .ThenInclude(p => p!.PlantImages)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                        .ThenInclude(d => d.NurseryPlantCombo)
                            .ThenInclude(npc => npc!.PlantCombo)
                                .ThenInclude(pc => pc!.PlantComboImages)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                        .ThenInclude(d => d.NurseryMaterial)
                            .ThenInclude(nm => nm!.Material)
                                .ThenInclude(m => m!.MaterialImages)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.Nursery)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.Shipper)
                .Include(o => o.Invoices)
                    .ThenInclude(i => i.InvoiceDetails)
                .Include(o => o.Payments);
        }
    }
}
