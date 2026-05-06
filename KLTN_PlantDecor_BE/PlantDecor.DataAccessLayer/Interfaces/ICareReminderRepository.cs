using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ICareReminderRepository : IGenericRepository<CareReminder>
    {
        Task<List<CareReminder>> GetAllWithDetailsAsync();
        Task<CareReminder?> GetByIdWithDetailsAsync(int id);
        Task<List<CareReminder>> GetByUserIdWithDetailsAsync(int userId);
        Task<List<CareReminder>> GetByUserIdAndReminderDateAsync(int userId, DateOnly reminderDate);
        Task<PaginatedResult<CareReminder>> GetByUserIdWithFiltersAsync(int userId, int? careType, Pagination pagination);
        Task<List<CareReminder>> GetByReminderDateWithUserPlantAsync(DateOnly reminderDate);
    }
}
