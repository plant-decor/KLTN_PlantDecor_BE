using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IInvoiceService
    {
        Task<InvoiceResponseDto> GetInvoiceByIdAsync(int invoiceId, int userId);
        Task<List<InvoiceResponseDto>> GetInvoicesByOrderIdAsync(int orderId, int userId);
        Task<InvoiceResponseDto> GenerateRemainingInvoiceAsync(int orderId);
    }
}
