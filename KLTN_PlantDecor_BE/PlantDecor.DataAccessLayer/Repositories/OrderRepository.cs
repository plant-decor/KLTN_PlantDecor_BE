using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
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
                .Where(o => o.UserId == userId
                && o.OrderType != (int)OrderTypeEnum.Service
                && o.OrderType != (int)OrderTypeEnum.Design);

            if (orderStatus.HasValue)
                query = query.Where(o => o.Status == orderStatus.Value);

            return await query
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<PaginatedResult<Order>> SearchDesignForOperatorAsync(
            int nurseryId,
            Pagination pagination,
            int? status = null)
        {
            var query = BuildDetailedQuery()
                .Where(o => (o.OrderType == (int)OrderTypeEnum.Design && 
                            o.DesignRegistration != null && 
                            o.DesignRegistration.NurseryId == nurseryId)
                        || (o.OrderType == (int)OrderTypeEnum.Service && 
                            o.ServiceRegistration != null && 
                            o.ServiceRegistration.NurseryCareService != null &&
                            o.ServiceRegistration.NurseryCareService.Nursery.Id == nurseryId));

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            query = query.OrderByDescending(o => o.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Order>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<Order>> SearchForConsultantAsync(
            Pagination pagination,
            int? status,
            int? orderType,
            int? paymentStrategy,
            DateTime? createdFrom,
            DateTime? createdTo,
            decimal? minTotalAmount,
            decimal? maxTotalAmount,
            string? customerEmail,
            OrderSortByEnum? sortBy,
            SortDirectionEnum? sortDirection)
        {
            var query = BuildDetailedQuery();

            if (status.HasValue)
                query = query.Where(o => o.Status == status.Value);

            if (orderType.HasValue)
                query = query.Where(o => o.OrderType == orderType.Value);

            if (paymentStrategy.HasValue)
                query = query.Where(o => o.PaymentStrategy == paymentStrategy.Value);

            if (createdFrom.HasValue)
                query = query.Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value >= createdFrom.Value);

            if (createdTo.HasValue)
                query = query.Where(o => o.CreatedAt.HasValue && o.CreatedAt.Value <= createdTo.Value);

            if (minTotalAmount.HasValue)
                query = query.Where(o => o.TotalAmount.HasValue && o.TotalAmount.Value >= minTotalAmount.Value);

            if (maxTotalAmount.HasValue)
                query = query.Where(o => o.TotalAmount.HasValue && o.TotalAmount.Value <= maxTotalAmount.Value);

            if (!string.IsNullOrWhiteSpace(customerEmail))
            {
                var term = customerEmail.Trim().ToLower();
                query = query.Where(o => o.Customer != null
                    && o.Customer.Email.ToLower().Contains(term));
            }

            var appliedSortBy = sortBy ?? OrderSortByEnum.CreatedAt;
            var appliedSortDirection = sortDirection ?? (sortBy.HasValue ? SortDirectionEnum.Asc : SortDirectionEnum.Desc);
            var isDesc = appliedSortDirection == SortDirectionEnum.Desc;

            query = appliedSortBy switch
            {
                OrderSortByEnum.TotalAmount => isDesc
                    ? query.OrderByDescending(o => o.TotalAmount)
                    : query.OrderBy(o => o.TotalAmount),
                OrderSortByEnum.Status => isDesc
                    ? query.OrderByDescending(o => o.Status)
                    : query.OrderBy(o => o.Status),
                _ => isDesc
                    ? query.OrderByDescending(o => o.CreatedAt)
                    : query.OrderBy(o => o.CreatedAt)
            };

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Order>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<List<Order>> GetPendingConfirmationOrdersOlderThanAsync(DateTime threshold)
        {
            return await _context.Orders
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                        .ThenInclude(d => d.CommonPlant)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                        .ThenInclude(d => d.PlantInstance)
                .Where(o => o.Status == (int)Enums.OrderStatusEnum.PendingConfirmation
                    && o.UpdatedAt.HasValue
                    && o.UpdatedAt.Value.Date <= threshold.Date)
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
                        .ThenInclude(d => d.CommonPlant)
                            .ThenInclude(cp => cp!.Plant)
                                .ThenInclude(p => p!.Categories)
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
                        .ThenInclude(d => d.PlantInstance)
                            .ThenInclude(pi => pi!.Plant)
                                .ThenInclude(p => p!.Categories)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                        .ThenInclude(d => d.NurseryPlantCombo)
                            .ThenInclude(npc => npc!.PlantCombo)
                                .ThenInclude(pc => pc!.PlantComboImages)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                        .ThenInclude(d => d.NurseryPlantCombo)
                            .ThenInclude(npc => npc!.PlantCombo)
                                .ThenInclude(pc => pc!.PlantComboItems)
                                    .ThenInclude(pci => pci.Plant)
                                        .ThenInclude(p => p!.Categories)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                        .ThenInclude(d => d.NurseryMaterial)
                            .ThenInclude(nm => nm!.Material)
                                .ThenInclude(m => m!.MaterialImages)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.Nursery)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.Shipper)
                .Include(o => o.DesignRegistration)
                    .ThenInclude(dr => dr!.Nursery)
                .Include(o => o.ServiceRegistration)
                    .ThenInclude(sr => sr!.NurseryCareService)
                        .ThenInclude(ncs => ncs!.Nursery)
                .Include(o => o.Invoices)
                    .ThenInclude(i => i.InvoiceDetails)
                .Include(o => o.Payments)
                .Include(o => o.Customer);
        }
    }
}
