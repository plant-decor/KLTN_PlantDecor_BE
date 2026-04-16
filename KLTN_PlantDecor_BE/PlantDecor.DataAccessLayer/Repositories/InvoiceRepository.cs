using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class InvoiceRepository : GenericRepository<Invoice>, IInvoiceRepository
    {
        public InvoiceRepository(PlantDecorContext context) : base(context) { }

        public async Task<Invoice?> GetByIdWithDetailsAsync(int invoiceId)
        {
            return await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .Include(i => i.Order)
                .FirstOrDefaultAsync(i => i.Id == invoiceId);
        }

        public async Task<List<Invoice>> GetByOrderIdAsync(int orderId)
        {
            return await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .Where(i => i.OrderId == orderId)
                .OrderByDescending(i => i.IssuedDate)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetPendingByUserIdAsync(int userId)
        {
            return await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .Include(i => i.Order)
                .Where(i => i.Order != null
                    && i.Order.UserId == userId
                    && i.Status == (int)InvoiceStatusEnum.Pending)
                .OrderByDescending(i => i.IssuedDate)
                .ToListAsync();
        }

        public async Task<List<Invoice>> GetPendingRemainingInvoicesForShipperAsync(int shipperId, int nurseryId)
        {
            return await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .Include(i => i.Order)
                .Where(i => i.Status == (int)InvoiceStatusEnum.Pending
                    && i.Type == (int)InvoiceTypeEnum.RemainingBalance
                    && i.Order != null
                    && i.Order.Status == (int)OrderStatusEnum.RemainingPaymentPending
                    && i.Order.NurseryOrders.Any(no => no.NurseryId == nurseryId
                        && no.ShipperId == shipperId
                        && no.Status == (int)OrderStatusEnum.Delivered))
                .OrderByDescending(i => i.IssuedDate)
                .ToListAsync();
        }

        public async Task<Invoice?> GetPendingByOrderIdAndTypeAsync(int orderId, int type)
        {
            return await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(i => i.OrderId == orderId
                    && i.Type == type
                    && i.Status == (int)InvoiceStatusEnum.Pending);
        }
    }
}
