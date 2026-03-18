using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IOrderService
    {
        Task<List<OrderResponseDto>> CreateOrderAsync(int userId, CreateOrderRequestDto request);
        Task<OrderResponseDto> GetOrderByIdAsync(int orderId, int userId);
        Task<List<OrderResponseDto>> GetMyOrdersAsync(int userId);
        Task<OrderResponseDto> CancelOrderAsync(int orderId, int userId);
    }
}
