using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IAIChatMessageRepository : IGenericRepository<AIChatMessage>
    {
        Task<AIChatMessage> AddUserMessageAsync(int sessionId, int userId, string content);
        Task<AIChatMessage> AddAssistantMessageAsync(int sessionId, int userId, string content, string? intent = null, bool isFallback = false, bool isPolicyResponse = false);
        Task<List<AIChatMessage>> GetSessionMessagesAsync(int sessionId, int userId, int pageNumber = 1, int pageSize = 50);
        Task<int> GetSessionMessagesCountAsync(int sessionId, int userId);
        Task<AIChatMessage?> GetLatestMessageAsync(int sessionId, int userId);
    }
}
