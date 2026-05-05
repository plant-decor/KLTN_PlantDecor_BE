using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IUserPlantService
    {
        Task<List<UserPlantResponseDto>> GetMyPlantsAsync(int userId);
        Task<List<CareReminderNotificationResponseDto>> GetMyCareRemindersAsync(int userId);
        Task<List<CareReminderNotificationResponseDto>> GetMyCareRemindersTodayAsync(int userId);
        Task AddPurchasedPlantsToMyPlantAsync(int orderId, DateTime purchasedAt);
    }
}