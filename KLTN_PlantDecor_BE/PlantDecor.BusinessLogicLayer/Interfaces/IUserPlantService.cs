using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IUserPlantService
    {
        Task<List<UserPlantResponseDto>> GetMyPlantsAsync(int userId);
        Task<UserPlantResponseDto> UpdateMyPlantAsync(int userId, int userPlantId, UpdateUserPlantRequestDto request);
        Task<PaginatedResult<CareReminderNotificationResponseDto>> GetMyCareRemindersAsync(int userId, int? careType, Pagination pagination);
        Task<List<CareReminderNotificationResponseDto>> GetMyCareRemindersTodayAsync(int userId);
        Task AddPurchasedPlantsToMyPlantAsync(int orderId, DateTime purchasedAt);
    }
}
