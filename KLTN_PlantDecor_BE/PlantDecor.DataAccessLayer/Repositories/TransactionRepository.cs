using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class TransactionRepository : GenericRepository<Transaction>, ITransactionRepository
    {
        public TransactionRepository(PlantDecorContext context) : base(context) { }

        public async Task<Transaction?> GetByTransactionIdAsync(string transactionId)
        {
            return await _context.Transactions
                .Include(t => t.Payment)
                .FirstOrDefaultAsync(t => t.TransactionId == transactionId);
        }

        public async Task<List<Transaction>> GetByPaymentIdAsync(int paymentId)
        {
            return await _context.Transactions
                .Where(t => t.PaymentId == paymentId)
                .ToListAsync();
        }
    }
}
