using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IChatSessionRepository : IGenericRepository<ChatSession>
    {
        Task<List<ChatSession>> GetUserConversationsAsync(int userId);
        Task<ChatSession?> GetConversationWithParticipantsAsync(int conversationId);
        Task<ChatSession?> CreateConversationAsync(int userId1, int userId2);
        Task<ChatSession?> FindExistingConversationAsync(int userId1, int userId2);
    }
}
