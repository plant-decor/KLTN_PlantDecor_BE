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
        Task<ConversationResponseDto?> GetConversationDetailsAsync(int userId, int conversationId, int pageNumber = 1, int pageSize = 30);
        Task<ConversationMessagesResponseDto> GetConversationMessagesAsync(int userId, int conversationId, int pageNumber = 1, int pageSize = 50);

        Task<ConversationResponseDto> CreateConversationAsync(int userId, int otherUserId);

        Task<ConversationResponseDto> StartSupportConversationAsync(int customerId, string firstMessage);
        Task<List<ConversationResponseDto>> GetWaitingSupportConversationsAsync();
        Task<List<ConversationResponseDto>> GetMyClaimedSupportConversationsAsync(int consultantId);
        Task<bool> ClaimSupportConversationAsync(int consultantId, int conversationId);
        Task<ConversationResponseDto> GetLatestActiveConversationAsync(int customerId);
        Task CloseConversationAsync(int userId, int conversationId);
    }
}
