using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IPaymentRepository : IGenericRepository<Payment>
    {
        Task<Payment?> GetByIdWithTransactionsAsync(int paymentId);
        Task<List<Payment>> GetByOrderIdAsync(int orderId);
        Task<List<Payment>> GetPendingWithTransactionsAsync();
    }
}
