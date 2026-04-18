using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IOrderRepository : IGenericRepository<Order>
    {
        Task<Order?> GetByIdWithDetailsAsync(int orderId);
        Task<List<Order>> GetByUserIdWithDetailsAsync(int userId, int? orderStatus = null);
        Task<List<Order>> GetPendingConfirmationOrdersOlderThanAsync(DateTime threshold);
    }
}
