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

        public async Task<decimal> GetNetPaidSystemRevenueAsync(DateTime fromInclusive, DateTime toExclusive)
        {
            var paidAmount = await BuildPaidPaymentDateRangeQuery(fromInclusive, toExclusive)
                .SumAsync(p => p.Amount ?? 0m);

            var refundedAmount = await BuildRefundedReturnItemDateRangeQuery(fromInclusive, toExclusive)
                .SumAsync(i => i.RefundedAmount ?? 0m);

            return paidAmount - refundedAmount;
        }

        public async Task<int> CountPaidSystemOrdersAsync(DateTime fromInclusive, DateTime toExclusive)
        {
            return await BuildPaidPaymentDateRangeQuery(fromInclusive, toExclusive)
                .Where(p => p.OrderId.HasValue)
                .Select(p => p.OrderId!.Value)
                .Distinct()
                .CountAsync();
        }

        public async Task<List<NurseryRevenueAggregate>> GetNetPaidRevenueByNurseryListAsync(DateTime fromInclusive, DateTime toExclusive)
        {
            var productRevenue = await BuildPaidPaymentDateRangeQuery(fromInclusive, toExclusive)
                .Where(p => p.Order != null
                    && p.Order.TotalAmount.HasValue
                    && p.Order.TotalAmount.Value > 0
                    && (p.Order.OrderType == (int)OrderTypeEnum.OtherProduct
                        || p.Order.OrderType == (int)OrderTypeEnum.OtherProductBuyNow
                        || p.Order.OrderType == (int)OrderTypeEnum.PlantInstance))
                .SelectMany(p => p.Order!.NurseryOrders.Select(no => new
                {
                    no.NurseryId,
                    NurseryName = no.Nursery.Name,
                    Revenue = (p.Amount ?? 0m) * (no.SubTotalAmount ?? 0m) / p.Order.TotalAmount!.Value,
                    OrderId = p.OrderId ?? 0
                }))
                .GroupBy(x => new { x.NurseryId, x.NurseryName })
                .Select(g => new NurseryRevenueAggregate
                {
                    NurseryId = g.Key.NurseryId,
                    NurseryName = g.Key.NurseryName ?? string.Empty,
                    Revenue = g.Sum(x => x.Revenue),
                    TotalOrders = g.Select(x => x.OrderId).Distinct().Count()
                })
                .ToListAsync();

            var serviceRevenue = await BuildPaidPaymentDateRangeQuery(fromInclusive, toExclusive)
                .Where(p => p.Order != null && p.Order.OrderType == (int)OrderTypeEnum.Service)
                .Join(_context.ServiceRegistrations,
                    payment => payment.OrderId,
                    registration => registration.OrderId,
                    (payment, registration) => new
                    {
                        payment,
                        registration
                    })
                .Where(x => x.registration.NurseryCareService != null)
                .GroupBy(x => new
                {
                    NurseryId = x.registration.NurseryCareService!.NurseryId,
                    NurseryName = x.registration.NurseryCareService.Nursery.Name
                })
                .Select(g => new NurseryRevenueAggregate
                {
                    NurseryId = g.Key.NurseryId,
                    NurseryName = g.Key.NurseryName ?? string.Empty,
                    Revenue = g.Sum(x => x.payment.Amount ?? 0m),
                    TotalOrders = g.Select(x => x.payment.OrderId ?? 0).Distinct().Count()
                })
                .ToListAsync();

            var designRevenue = await BuildPaidPaymentDateRangeQuery(fromInclusive, toExclusive)
                .Where(p => p.Order != null && p.Order.OrderType == (int)OrderTypeEnum.Design)
                .Join(_context.DesignRegistrations,
                    payment => payment.OrderId,
                    registration => registration.OrderId,
                    (payment, registration) => new
                    {
                        payment,
                        registration
                    })
                .GroupBy(x => new
                {
                    x.registration.NurseryId,
                    NurseryName = x.registration.Nursery.Name
                })
                .Select(g => new NurseryRevenueAggregate
                {
                    NurseryId = g.Key.NurseryId,
                    NurseryName = g.Key.NurseryName ?? string.Empty,
                    Revenue = g.Sum(x => x.payment.Amount ?? 0m),
                    TotalOrders = g.Select(x => x.payment.OrderId ?? 0).Distinct().Count()
                })
                .ToListAsync();

            var refundDeductions = await BuildRefundedReturnItemDateRangeQuery(fromInclusive, toExclusive)
                .GroupBy(i => new
                {
                    i.NurseryOrderDetail.NurseryOrder.NurseryId,
                    NurseryName = i.NurseryOrderDetail.NurseryOrder.Nursery.Name
                })
                .Select(g => new NurseryRevenueAggregate
                {
                    NurseryId = g.Key.NurseryId,
                    NurseryName = g.Key.NurseryName ?? string.Empty,
                    Revenue = -g.Sum(x => x.RefundedAmount ?? 0m),
                    TotalOrders = 0
                })
                .ToListAsync();

            return productRevenue
                .Concat(serviceRevenue)
                .Concat(designRevenue)
                .Concat(refundDeductions)
                .GroupBy(x => new { x.NurseryId, x.NurseryName })
                .Select(g => new NurseryRevenueAggregate
                {
                    NurseryId = g.Key.NurseryId,
                    NurseryName = g.Key.NurseryName,
                    Revenue = g.Sum(x => x.Revenue),
                    TotalOrders = g.Sum(x => x.TotalOrders)
                })
                .OrderByDescending(x => x.Revenue)
                .ToList();
        }

        public async Task<List<OrderStatusAggregate>> GetOrderStatusSummaryAsync(DateTime fromInclusive, DateTime toExclusive, int? nurseryId = null)
        {
            var query = BuildOrderDateRangeQuery(fromInclusive, toExclusive);

            if (nurseryId.HasValue)
                query = query.Where(no => no.NurseryId == nurseryId.Value);

            return await query
                .GroupBy(no => no.Status ?? 0)
                .Select(g => new OrderStatusAggregate
                {
                    Status = g.Key,
                    TotalOrders = g.Count()
                })
                .OrderBy(x => x.Status)
                .ToListAsync();
        }

        public async Task<int> CountFailedOrdersAsync(DateTime fromInclusive, DateTime toExclusive, int? nurseryId = null)
        {
            var query = BuildOrderDateRangeQuery(fromInclusive, toExclusive)
                .Where(no => no.Status == (int)OrderStatusEnum.Failed);

            if (nurseryId.HasValue)
                query = query.Where(no => no.NurseryId == nurseryId.Value);

            return await query.CountAsync();
        }

        public async Task<List<TopProductAggregate>> GetTopProductsAsync(DateTime fromInclusive, DateTime toExclusive, int? nurseryId, int limit)
        {
            var completedOrders = BuildCompletedRevenueQuery(fromInclusive, toExclusive);

            if (nurseryId.HasValue)
                completedOrders = completedOrders.Where(no => no.NurseryId == nurseryId.Value);

            return await completedOrders
                .SelectMany(no => no.NurseryOrderDetails)
                .GroupBy(d => new
                {
                    ProductType = d.CommonPlantId.HasValue
                        ? "CommonPlant"
                        : d.PlantInstanceId.HasValue
                            ? "PlantInstance"
                            : d.NurseryPlantComboId.HasValue
                                ? "NurseryPlantCombo"
                                : "NurseryMaterial",
                    ProductId = d.CommonPlantId
                        ?? d.PlantInstanceId
                        ?? d.NurseryPlantComboId
                        ?? d.NurseryMaterialId
                        ?? 0,
                    ProductName = d.ItemName
                })
                .Select(g => new TopProductAggregate
                {
                    ProductType = g.Key.ProductType,
                    ProductId = g.Key.ProductId,
                    ProductName = g.Key.ProductName ?? string.Empty,
                    TotalQuantity = g.Sum(x => x.Quantity ?? 0),
                    TotalRevenue = g.Sum(x => x.Amount ?? 0m)
                })
                .OrderByDescending(x => x.TotalRevenue)
                .ThenByDescending(x => x.TotalQuantity)
                .Take(limit)
                .ToListAsync();
        }

        private IQueryable<NurseryOrder> BuildCompletedRevenueQuery(DateTime fromInclusive, DateTime toExclusive)
        {
            return _context.NurseryOrders
                .Where(no => no.Status == (int)OrderStatusEnum.Completed)
                .Where(no => (no.Order!.CompletedAt ?? no.UpdatedAt ?? no.CreatedAt) >= fromInclusive
                    && (no.Order!.CompletedAt ?? no.UpdatedAt ?? no.CreatedAt) < toExclusive);
        }

        private IQueryable<Payment> BuildPaidPaymentDateRangeQuery(DateTime fromInclusive, DateTime toExclusive)
        {
            return _context.Payments
                .Where(p => p.Status == (int)PaymentStatusEnum.Paid)
                .Where(p => p.PaidAt.HasValue && p.PaidAt.Value >= fromInclusive && p.PaidAt.Value < toExclusive);
        }

        private IQueryable<ReturnTicketItem> BuildRefundedReturnItemDateRangeQuery(DateTime fromInclusive, DateTime toExclusive)
        {
            return _context.ReturnTicketItems
                .Where(i => i.Status == (int)ReturnTicketItemStatusEnum.Refunded)
                .Where(i => i.RefundedAt.HasValue && i.RefundedAt.Value >= fromInclusive && i.RefundedAt.Value < toExclusive);
        }

        private IQueryable<NurseryOrder> BuildOrderDateRangeQuery(DateTime fromInclusive, DateTime toExclusive)
        {
            return _context.NurseryOrders
                .Where(no => (no.CreatedAt ?? no.UpdatedAt ?? no.Order!.CreatedAt) >= fromInclusive
                    && (no.CreatedAt ?? no.UpdatedAt ?? no.Order!.CreatedAt) < toExclusive);
        }
    }
}
