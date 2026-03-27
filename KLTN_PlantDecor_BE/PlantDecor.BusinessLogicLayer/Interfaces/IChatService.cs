using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IChatService
    {
        Task<bool> IsParticipantAsync(int userId, int conversationId);
        Task<ChatMessage> SendMessageAsync(int userId, int conversationId, string content);
        Task<List<ConversationResponseDto>> GetUserConversationsAsync(int userId);
        Task<ConversationResponseDto?> GetConversationDetailsAsync(int userId, int conversationId);
        Task<ConversationMessagesResponseDto> GetConversationMessagesAsync(int userId, int conversationId, int pageNumber = 1, int pageSize = 50);
        Task<ConversationResponseDto> CreateConversationAsync(int userId, int otherUserId);
    }
}
