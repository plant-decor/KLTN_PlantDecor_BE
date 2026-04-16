using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IInvoiceRepository : IGenericRepository<Invoice>
    {
        Task<Invoice?> GetByIdWithDetailsAsync(int invoiceId);
        Task<List<Invoice>> GetByOrderIdAsync(int orderId);
        Task<List<Invoice>> GetPendingByUserIdAsync(int userId);
        Task<List<Invoice>> GetPendingRemainingInvoicesForShipperAsync(int shipperId, int nurseryId);
        Task<Invoice?> GetPendingByOrderIdAndTypeAsync(int orderId, int type);
    }
}
