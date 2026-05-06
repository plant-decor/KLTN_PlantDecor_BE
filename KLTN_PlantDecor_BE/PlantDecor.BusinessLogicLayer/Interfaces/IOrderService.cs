using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrderAsync(int userId, CreateOrderRequestDto request);
        Task<OrderResponseDto> GetOrderByIdAsync(int orderId, int userId);
        Task<List<OrderResponseDto>> GetMyOrdersAsync(int userId, OrderStatusEnum? orderStatus = null);
        Task<PaginatedResult<OrderResponseDto>> GetDesignOrdersForOperatorAsync(
            int operatorId,
            Pagination pagination,
            OrderStatusEnum? status = null);
        Task<OrderResponseDto> CancelOrderAsync(int orderId, int userId);
        Task<OrderResponseDto> MarkOrderAsDeliveredAsync(int orderId);
        Task<List<OrderResponseDto>> GetOrdersByEmailAsync(string email);
        Task<PaginatedResult<OrderResponseDto>> GetOrdersForConsultantAsync(
            ConsultantOrderSearchRequestDto request,
            Pagination pagination);
        Task<OrderResponseDto> GetOrderByIdForConsultantAsync(int orderId);
    }
}
