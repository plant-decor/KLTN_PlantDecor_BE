using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class NurseryOrderRepository : GenericRepository<NurseryOrder>, INurseryOrderRepository
    {
        public NurseryOrderRepository(PlantDecorContext context) : base(context) { }

        public async Task<List<NurseryOrder>> GetByNurseryIdAsync(int nurseryId)
        {
            return await _context.NurseryOrders
                .Include(no => no.Nursery)
                .Include(no => no.Shipper)
                .Include(no => no.NurseryOrderDetails)
                .Where(no => no.NurseryId == nurseryId)
                .ToListAsync();
        }

        public async Task<List<NurseryOrder>> GetByShipperAndNurseryAsync(int shipperId, int nurseryId, List<int>? statuses = null)
        {
            var query = _context.NurseryOrders
                .Include(no => no.Nursery)
                .Include(no => no.Shipper)
                .Include(no => no.NurseryOrderDetails)
                .Where(no => no.ShipperId == shipperId && no.NurseryId == nurseryId);

            if (statuses != null && statuses.Count > 0)
                query = query.Where(no => no.Status.HasValue && statuses.Contains(no.Status.Value));

            return await query.ToListAsync();
        }

        public async Task<NurseryOrder?> GetByIdWithDetailsAsync(int nurseryOrderId)
        {
            return await _context.NurseryOrders
                .Include(no => no.Nursery)
                .Include(no => no.Shipper)
                .Include(no => no.NurseryOrderDetails)
                .FirstOrDefaultAsync(no => no.Id == nurseryOrderId);
        }

        public async Task<(List<NurseryOrder> Items, int TotalCount)> GetByShipperAndNurseryPagedAsync(int shipperId, int nurseryId, int? status, int skip, int take)
        {
            var query = _context.NurseryOrders
                .Include(no => no.Nursery)
                .Include(no => no.Shipper)
                .Include(no => no.NurseryOrderDetails)
                .Where(no => no.ShipperId == shipperId && no.NurseryId == nurseryId)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(no => no.Status == status.Value);

            query = query.OrderByDescending(no => no.UpdatedAt ?? no.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip(skip).Take(take).ToListAsync();

            return (items, totalCount);
        }

        public async Task<(List<NurseryOrder> Items, int TotalCount)> GetByNurseryIdPagedAsync(int nurseryId, int? status, int skip, int take)
        {
            var query = _context.NurseryOrders
                .Include(no => no.Nursery)
                .Include(no => no.Shipper)
                .Include(no => no.NurseryOrderDetails)
                .Where(no => no.NurseryId == nurseryId)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(no => no.Status == status.Value);

            query = query.OrderByDescending(no => no.UpdatedAt ?? no.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip(skip).Take(take).ToListAsync();

            return (items, totalCount);
        }
    }
}
