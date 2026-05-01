using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface INurseryOrderRepository : IGenericRepository<NurseryOrder>
    {
        Task<List<NurseryOrder>> GetByNurseryIdAsync(int nurseryId);
        Task<List<NurseryOrder>> GetByShipperAndNurseryAsync(int shipperId, int nurseryId, List<int>? statuses = null);
        Task<NurseryOrder?> GetByIdWithDetailsAsync(int nurseryOrderId);
        Task<(List<NurseryOrder> Items, int TotalCount)> GetByShipperAndNurseryPagedAsync(int shipperId, int nurseryId, int? status, int skip, int take);
        Task<(List<NurseryOrder> Items, int TotalCount)> GetByNurseryIdPagedAsync(int nurseryId, int? status, int skip, int take);
        Task<decimal> GetCompletedRevenueByNurseryAsync(int nurseryId, DateTime fromInclusive, DateTime toExclusive);
        Task<int> CountCompletedOrdersByNurseryAsync(int nurseryId, DateTime fromInclusive, DateTime toExclusive);
        Task<decimal> GetCompletedSystemRevenueAsync(DateTime fromInclusive, DateTime toExclusive);
        Task<int> CountCompletedSystemOrdersAsync(DateTime fromInclusive, DateTime toExclusive);
        Task<List<NurseryRevenueAggregate>> GetCompletedRevenueByNurseryListAsync(DateTime fromInclusive, DateTime toExclusive);
        Task<List<OrderStatusAggregate>> GetOrderStatusSummaryAsync(DateTime fromInclusive, DateTime toExclusive, int? nurseryId = null);
        Task<int> CountFailedOrdersAsync(DateTime fromInclusive, DateTime toExclusive, int? nurseryId = null);
        Task<List<TopProductAggregate>> GetTopProductsAsync(DateTime fromInclusive, DateTime toExclusive, int? nurseryId, int limit);
    }
}
