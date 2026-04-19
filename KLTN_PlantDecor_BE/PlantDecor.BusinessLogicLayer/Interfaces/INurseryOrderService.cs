using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface INurseryOrderService
    {
        Task<PaginatedResult<NurseryOrderResponseDto>> GetMyNurseryOrdersAsync(int currentUserId, int? status, Pagination pagination);
        Task<List<InvoiceResponseDto>> GetPendingInvoicesForMyNurseryAsync(int currentUserId);
        Task<PaginatedResult<NurseryOrderResponseDto>> GetNurseryOrdersAsync(int currentUserId, int? status, Pagination pagination);
        Task<NurseryOrderResponseDto> GetNurseryOrderDetailForManagerAsync(int currentUserId, int nurseryOrderId);
        Task<NurseryOrderResponseDto> GetNurseryOrderDetailForShipperAsync(int currentUserId, int nurseryOrderId);
        Task<NurseryOrderResponseDto> StartShippingAsync(int currentUserId, int nurseryOrderId, StartShippingRequestDto request);
        Task<NurseryOrderResponseDto> MarkDeliveredAsync(int currentUserId, int nurseryOrderId, MarkDeliveredRequestDto request);
        Task<NurseryOrderResponseDto> MarkDeliveryFailedAsync(int currentUserId, int nurseryOrderId, MarkDeliveryFailedRequestDto request);
    }
}
