using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IAIChatSessionRepository : IGenericRepository<AIChatSession>
    {
        Task<AIChatSession> CreateSessionAsync(int userId, string? title = null);
        Task<AIChatSession?> GetByIdAndUserAsync(int sessionId, int userId);
        Task<List<AIChatSession>> GetUserSessionsAsync(int userId, int pageNumber = 1, int pageSize = 20);
        Task<int> GetUserSessionsCountAsync(int userId);
        Task<bool> CloseSessionAsync(int sessionId, int userId);
        Task<AIChatSession?> UpdateTitleAsync(int sessionId, int userId, string? title);
    }
}
