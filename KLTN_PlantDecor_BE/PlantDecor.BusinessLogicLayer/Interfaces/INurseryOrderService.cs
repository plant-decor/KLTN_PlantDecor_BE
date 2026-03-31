using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface INurseryOrderService
    {
        Task<List<NurseryOrderResponseDto>> GetMyNurseryOrdersAsync(int currentUserId);
        Task<NurseryOrderResponseDto> StartShippingAsync(int currentUserId, int nurseryOrderId, StartShippingRequestDto request);
        Task<NurseryOrderResponseDto> MarkDeliveredAsync(int currentUserId, int nurseryOrderId, MarkDeliveredRequestDto request);
    }
}
