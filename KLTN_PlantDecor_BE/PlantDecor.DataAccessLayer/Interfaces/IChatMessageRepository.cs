using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IChatMessageRepository : IGenericRepository<ChatMessage>
    {
        Task<List<ChatMessage>> GetConversationMessagesAsync(int conversationId, int pageNumber = 1, int pageSize = 50);
        Task<int> GetTotalMessagesCountAsync(int conversationId);
        Task<ChatMessage?> GetLatestMessageAsync(int conversationId);
    }
}
