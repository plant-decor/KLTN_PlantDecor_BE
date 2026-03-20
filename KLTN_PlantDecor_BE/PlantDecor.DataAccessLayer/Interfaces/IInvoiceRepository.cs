using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IInvoiceRepository : IGenericRepository<Invoice>
    {
        Task<Invoice?> GetByIdWithDetailsAsync(int invoiceId);
        Task<List<Invoice>> GetByOrderIdAsync(int orderId);
        Task<Invoice?> GetPendingByOrderIdAndTypeAsync(int orderId, int type);
    }
}
