using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ICareReminderRepository : IGenericRepository<CareReminder>
    {
        Task<List<CareReminder>> GetAllWithDetailsAsync();
        Task<CareReminder?> GetByIdWithDetailsAsync(int id);
        Task<List<CareReminder>> GetByUserIdWithDetailsAsync(int userId);
    }
}
