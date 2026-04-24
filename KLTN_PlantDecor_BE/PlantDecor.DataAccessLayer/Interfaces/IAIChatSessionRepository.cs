using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IAIChatSessionRepository : IGenericRepository<AIChatSession>
    {
        Task<AIChatSession> CreateSessionAsync(int userId, string? title = null);
        Task<AIChatSession?> GetByIdAndUserAsync(int sessionId, int userId);
        Task<List<AIChatSession>> GetUserSessionsAsync(int userId, int pageNumber = 1, int pageSize = 20);
        Task<bool> CloseSessionAsync(int sessionId, int userId);
    }
}
