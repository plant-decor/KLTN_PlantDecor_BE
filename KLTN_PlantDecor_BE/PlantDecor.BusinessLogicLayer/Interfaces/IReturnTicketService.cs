using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using Microsoft.AspNetCore.Http;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IReturnTicketService
    {
        Task<ReturnTicketResponseDto> CreateReturnTicketAsync(int customerId, CreateReturnTicketRequestDto request);
        Task<List<ReturnTicketResponseDto>> GetMyReturnTicketsAsync(int customerId);
        Task<ReturnTicketItemResponseDto> UploadReturnTicketItemImagesAsync(int customerId, int returnTicketId, int returnTicketItemId, List<IFormFile> files);
    }
}
