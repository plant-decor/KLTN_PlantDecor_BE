using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class NurseryOrderRepository : GenericRepository<NurseryOrder>, INurseryOrderRepository
    {
        public NurseryOrderRepository(PlantDecorContext context) : base(context) { }

        private IQueryable<NurseryOrder> BuildDetailedQuery()
        {
            return _context.NurseryOrders
                .Include(no => no.Nursery)
                .Include(no => no.Shipper)
                .Include(no => no.Order)
                    .ThenInclude(o => o.Customer)
                .Include(no => no.NurseryOrderDetails)
                    .ThenInclude(d => d.CommonPlant)
                        .ThenInclude(cp => cp!.Plant)
                            .ThenInclude(p => p!.PlantImages)
                .Include(no => no.NurseryOrderDetails)
                    .ThenInclude(d => d.PlantInstance)
                        .ThenInclude(pi => pi!.PlantImages)
                .Include(no => no.NurseryOrderDetails)
                    .ThenInclude(d => d.PlantInstance)
                        .ThenInclude(pi => pi!.Plant)
                            .ThenInclude(p => p!.PlantImages)
                .Include(no => no.NurseryOrderDetails)
                    .ThenInclude(d => d.NurseryPlantCombo)
                        .ThenInclude(npc => npc!.PlantCombo)
                            .ThenInclude(pc => pc!.PlantComboImages)
                .Include(no => no.NurseryOrderDetails)
                    .ThenInclude(d => d.NurseryMaterial)
                        .ThenInclude(nm => nm!.Material)
                            .ThenInclude(m => m!.MaterialImages);
        }

        public async Task<List<NurseryOrder>> GetByNurseryIdAsync(int nurseryId)
        {
            return await BuildDetailedQuery()
                .Where(no => no.NurseryId == nurseryId)
                .ToListAsync();
        }

        public async Task<List<NurseryOrder>> GetByShipperAndNurseryAsync(int shipperId, int nurseryId, List<int>? statuses = null)
        {
            var query = BuildDetailedQuery()
                .Where(no => no.ShipperId == shipperId && no.NurseryId == nurseryId);

            if (statuses != null && statuses.Count > 0)
                query = query.Where(no => no.Status.HasValue && statuses.Contains(no.Status.Value));

            return await query.ToListAsync();
        }

        public async Task<NurseryOrder?> GetByIdWithDetailsAsync(int nurseryOrderId)
        {
            return await BuildDetailedQuery()
                .FirstOrDefaultAsync(no => no.Id == nurseryOrderId);
        }

        public async Task<(List<NurseryOrder> Items, int TotalCount)> GetByShipperAndNurseryPagedAsync(int shipperId, int nurseryId, int? status, int skip, int take)
        {
            var query = BuildDetailedQuery()
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
            var query = BuildDetailedQuery()
                .Where(no => no.NurseryId == nurseryId)
                .AsQueryable();

            if (status.HasValue)
                query = query.Where(no => no.Status == status.Value);

            query = query.OrderByDescending(no => no.UpdatedAt ?? no.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip(skip).Take(take).ToListAsync();

            return (items, totalCount);
        }

        public async Task<decimal> GetCompletedRevenueByNurseryAsync(int nurseryId, DateTime fromInclusive, DateTime toExclusive)
        {
            return await BuildCompletedRevenueQuery(fromInclusive, toExclusive)
                .Where(no => no.NurseryId == nurseryId)
                .SumAsync(no => no.SubTotalAmount ?? 0m);
        }

        public async Task<int> CountCompletedOrdersByNurseryAsync(int nurseryId, DateTime fromInclusive, DateTime toExclusive)
        {
            return await BuildCompletedRevenueQuery(fromInclusive, toExclusive)
                .Where(no => no.NurseryId == nurseryId)
                .CountAsync();
        }

        public async Task<decimal> GetCompletedSystemRevenueAsync(DateTime fromInclusive, DateTime toExclusive)
        {
            return await BuildCompletedRevenueQuery(fromInclusive, toExclusive)
                .SumAsync(no => no.SubTotalAmount ?? 0m);
        }

        public async Task<int> CountCompletedSystemOrdersAsync(DateTime fromInclusive, DateTime toExclusive)
        {
            return await BuildCompletedRevenueQuery(fromInclusive, toExclusive)
                .CountAsync();
        }

        public async Task<List<NurseryRevenueAggregate>> GetCompletedRevenueByNurseryListAsync(DateTime fromInclusive, DateTime toExclusive)
        {
            return await BuildCompletedRevenueQuery(fromInclusive, toExclusive)
                .GroupBy(no => new
                {
                    no.NurseryId,
                    NurseryName = no.Nursery.Name
                })
                .Select(g => new NurseryRevenueAggregate
                {
                    NurseryId = g.Key.NurseryId,
                    NurseryName = g.Key.NurseryName ?? string.Empty,
                    Revenue = g.Sum(x => x.SubTotalAmount ?? 0m),
                    TotalOrders = g.Count()
                })
                .OrderByDescending(x => x.Revenue)
                .ToListAsync();
        }

        private IQueryable<NurseryOrder> BuildCompletedRevenueQuery(DateTime fromInclusive, DateTime toExclusive)
        {
            return _context.NurseryOrders
                .Where(no => no.Status == (int)OrderStatusEnum.Completed)
                .Where(no => (no.Order!.CompletedAt ?? no.UpdatedAt ?? no.CreatedAt) >= fromInclusive
                    && (no.Order!.CompletedAt ?? no.UpdatedAt ?? no.CreatedAt) < toExclusive);
        }
    }
}
