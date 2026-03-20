using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class PaymentRepository : GenericRepository<Payment>, IPaymentRepository
    {
        public PaymentRepository(PlantDecorContext context) : base(context) { }

        public async Task<Payment?> GetByIdWithTransactionsAsync(int paymentId)
        {
            return await _context.Payments
                .Include(p => p.Transactions)
                .FirstOrDefaultAsync(p => p.Id == paymentId);
        }

        public async Task<List<Payment>> GetByOrderIdAsync(int orderId)
        {
            return await _context.Payments
                .Include(p => p.Transactions)
                .Where(p => p.OrderId == orderId)
                .ToListAsync();
        }

        public async Task<List<Payment>> GetPendingWithTransactionsAsync()
        {
            return await _context.Payments
                .Include(p => p.Transactions)
                .Where(p => p.Status == (int)PaymentStatusEnum.Pending)
                .ToListAsync();
        }
    }
}
