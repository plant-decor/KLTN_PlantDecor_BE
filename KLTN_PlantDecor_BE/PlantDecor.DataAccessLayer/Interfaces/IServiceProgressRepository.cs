using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IServiceProgressRepository : IGenericRepository<ServiceProgress>
    {
        Task<List<ServiceProgress>> GetByServiceRegistrationIdAsync(int serviceRegistrationId);
        Task<List<ServiceProgress>> GetByCaretakerAndDateAsync(int caretakerId, DateOnly date);
        Task<ServiceProgress?> GetByIdWithDetailsAsync(int id);
        Task<List<ServiceProgress>> GetByNurseryAndDateAsync(int nurseryId, DateOnly date);
        Task<List<ServiceProgress>> GetByCaretakerAndDateRangeAsync(int nurseryId, int caretakerId, DateOnly from, DateOnly to);
        Task<List<ServiceProgress>> GetByCaretakerSelfDateRangeAsync(int caretakerId, DateOnly from, DateOnly to);
    }
}
