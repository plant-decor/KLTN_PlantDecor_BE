using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
        Task<Transaction?> GetByTransactionIdAsync(string transactionId);
        Task<List<Transaction>> GetByPaymentIdAsync(int paymentId);
        Task<List<Transaction>> GetExpiredPendingTransactionsAsync();
    }
}
