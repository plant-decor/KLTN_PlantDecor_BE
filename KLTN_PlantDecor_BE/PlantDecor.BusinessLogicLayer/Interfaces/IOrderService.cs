using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IOrderService
    {
        Task<OrderResponseDto> CreateOrderAsync(int userId, CreateOrderRequestDto request);
        Task<OrderResponseDto> GetOrderByIdAsync(int orderId, int userId);
        Task<List<OrderResponseDto>> GetMyOrdersAsync(int userId, OrderStatusEnum? orderStatus = null);
        Task<OrderResponseDto> CancelOrderAsync(int orderId, int userId);
        Task<OrderResponseDto> MarkOrderAsDeliveredAsync(int orderId);
        Task<List<OrderResponseDto>> GetOrdersByEmailAsync(string email);
    }
}
