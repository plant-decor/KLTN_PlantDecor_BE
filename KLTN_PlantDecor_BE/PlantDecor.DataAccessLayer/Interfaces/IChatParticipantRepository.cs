using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IChatParticipantRepository : IGenericRepository<ChatParticipant>
    {
        Task<bool> IsParticipantAsync(int userId, int conversationId);
        Task<List<ChatParticipant>> GetConversationParticipantsAsync(int conversationId);
        Task AddParticipantAsync(int userId, int conversationId);
    }
}
