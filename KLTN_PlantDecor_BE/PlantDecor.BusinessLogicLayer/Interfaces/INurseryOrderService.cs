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
        Task<List<NurseryOrderShipperResponseDto>> GetNurseryShippersForManagerAsync(int currentUserId);
        Task<NurseryOrderResponseDto> UpdateNurseryOrderShipperForManagerAsync(int currentUserId, int nurseryOrderId, int shipperId);
        Task<NurseryOrderResponseDto> MarkNurseryOrderCompletedForManagerAsync(int currentUserId, int nurseryOrderId);
        Task<NurseryOrderResponseDto> GetNurseryOrderDetailForShipperAsync(int currentUserId, int nurseryOrderId);
        Task<NurseryOrderResponseDto> StartShippingAsync(int currentUserId, int nurseryOrderId, StartShippingRequestDto request);
        Task<NurseryOrderResponseDto> MarkDeliveredAsync(int currentUserId, int nurseryOrderId, MarkDeliveredRequestDto request);
        Task<NurseryOrderResponseDto> MarkDeliveryFailedAsync(int currentUserId, int nurseryOrderId, MarkDeliveryFailedRequestDto request);
        Task<RevenueSummaryResponseDto> GetMyNurseryRevenueSummaryAsync(int currentUserId, DateTime from, DateTime to);
        Task<RevenueSummaryResponseDto> GetSystemRevenueSummaryAsync(int currentUserId, DateTime from, DateTime to);
        Task<List<NurseryRevenueItemResponseDto>> GetSystemRevenueByNurseryAsync(int currentUserId, DateTime from, DateTime to);
        Task<OrderStatusSummaryResponseDto> GetMyNurseryOrderStatusSummaryAsync(int currentUserId, DateTime from, DateTime to);
        Task<OrderStatusSummaryResponseDto> GetSystemOrderStatusSummaryAsync(int currentUserId, DateTime from, DateTime to);
        Task<FailedOrderSummaryResponseDto> GetMyNurseryFailedOrdersSummaryAsync(int currentUserId, DateTime from, DateTime to);
        Task<FailedOrderSummaryResponseDto> GetSystemFailedOrdersSummaryAsync(int currentUserId, DateTime from, DateTime to);
        Task<List<TopProductResponseDto>> GetMyNurseryTopProductsAsync(int currentUserId, DateTime from, DateTime to, int limit = 10);
        Task<List<TopProductResponseDto>> GetSystemTopProductsAsync(int currentUserId, DateTime from, DateTime to, int limit = 10);
    }
}
