using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ICareReminderService
    {
        Task<CareReminderResponseDto> CreateForUserAsync(int userId, CreateCareReminderRequestDto request);
        Task<CareReminderResponseDto> UpdateForUserAsync(int userId, int id, UpdateCareReminderRequestDto request);
        Task DeleteForUserAsync(int userId, int id);
    }
}
