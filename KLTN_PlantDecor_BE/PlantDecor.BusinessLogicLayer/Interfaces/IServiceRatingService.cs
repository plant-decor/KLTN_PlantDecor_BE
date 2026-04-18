using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IServiceRatingService
    {
        Task<ServiceRatingResponseDto> CreateRatingAsync(int userId, CreateServiceRatingRequestDto request);
        Task<ServiceRatingResponseDto> GetByRegistrationIdAsync(int registrationId);
    }
}
