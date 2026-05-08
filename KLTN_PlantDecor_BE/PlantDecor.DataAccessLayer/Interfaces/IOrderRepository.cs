using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<Order?> GetByIdWithDetailsAsync(int orderId);
        Task<List<Order>> GetByUserIdWithDetailsAsync(int userId, int? orderStatus = null);
        Task<PaginatedResult<Order>> SearchDesignForOperatorAsync(
            int nurseryId,
            Pagination pagination,
            int? status = null);
        Task<PaginatedResult<Order>> SearchForConsultantAsync(
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
            SortDirectionEnum? sortDirection);
        Task<List<Order>> GetPendingConfirmationOrdersOlderThanAsync(DateTime threshold);
    }
}
