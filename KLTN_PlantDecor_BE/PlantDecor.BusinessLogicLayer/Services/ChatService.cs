using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;

        public ChatService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> IsParticipantAsync(int userId, int conversationId)
        {
            return await _unitOfWork.ChatParticipantRepository.IsParticipantAsync(userId, conversationId);
        }

        public async Task<ChatMessage> SendMessageAsync(int userId, int conversationId, string content)
        {
            var message = new ChatMessage
            {
                ChatSessionId = conversationId,
                Sender = userId,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.ChatMessageRepository.CreateAsync(message);
            return message;
        }

        public async Task<List<ConversationResponseDto>> GetUserConversationsAsync(int userId)
        {
            var conversations = await _unitOfWork.ChatSessionRepository.GetUserConversationsAsync(userId);

            var conversationDtos = new List<ConversationResponseDto>();

            foreach (var conv in conversations)
            {
                var latestMessage = await _unitOfWork.ChatMessageRepository.GetLatestMessageAsync(conv.Id);

                conversationDtos.Add(new ConversationResponseDto
                {
                    Id = conv.Id,
                    Status = conv.Status,
                    StartedAt = conv.StartedAt,
                    EndedAt = conv.EndedAt,
                    Participants = conv.ChatParticipants.Select(p => new ParticipantResponseDto
                    {
                        UserId = p.UserId,
                        FullName = p.User?.UserProfile?.FullName,
                        Email = p.User?.Email,
                        PhoneNumber = p.User?.PhoneNumber,
                        AvatarUrl = p.User?.AvatarUrl,
                        JoinedAt = p.JoinedAt
                    }).ToList(),
                    LatestMessage = latestMessage != null ? new MessageResponseDto
                    {
                        Id = latestMessage.Id,
                        ChatSessionId = latestMessage.ChatSessionId,
                        SenderId = latestMessage.Sender,
                        Content = latestMessage.Content,
                        CreatedAt = latestMessage.CreatedAt
                    } : null
                });
            }

            return conversationDtos;
        }

        public async Task<ConversationResponseDto?> GetConversationDetailsAsync(int userId, int conversationId)
        {
            // Check if user is participant
            var isParticipant = await _unitOfWork.ChatParticipantRepository.IsParticipantAsync(userId, conversationId);
            if (!isParticipant)
                throw new ForbiddenException("You are not a participant of this conversation");

            var conversation = await _unitOfWork.ChatSessionRepository.GetConversationWithParticipantsAsync(conversationId);
            if (conversation == null)
                throw new NotFoundException("Conversation not found");

            var latestMessage = await _unitOfWork.ChatMessageRepository.GetLatestMessageAsync(conversationId);

            return new ConversationResponseDto
            {
                Id = conversation.Id,
                Status = conversation.Status,
                StartedAt = conversation.StartedAt,
                EndedAt = conversation.EndedAt,
                Participants = conversation.ChatParticipants.Select(p => new ParticipantResponseDto
                {
                    UserId = p.UserId,
                    FullName = p.User?.UserProfile?.FullName,
                    Email = p.User?.Email,
                    PhoneNumber = p.User?.PhoneNumber,
                    AvatarUrl = p.User?.AvatarUrl,
                    JoinedAt = p.JoinedAt
                }).ToList(),
                LatestMessage = latestMessage != null ? new MessageResponseDto
                {
                    Id = latestMessage.Id,
                    ChatSessionId = latestMessage.ChatSessionId,
                    SenderId = latestMessage.Sender,
                    Content = latestMessage.Content,
                    CreatedAt = latestMessage.CreatedAt
                } : null
            };
        }

        public async Task<ConversationMessagesResponseDto> GetConversationMessagesAsync(int userId, int conversationId, int pageNumber = 1, int pageSize = 50)
        {
            // Check if user is participant
            var isParticipant = await _unitOfWork.ChatParticipantRepository.IsParticipantAsync(userId, conversationId);
            if (!isParticipant)
                throw new ForbiddenException("You are not a participant of this conversation");

            var messages = await _unitOfWork.ChatMessageRepository.GetConversationMessagesAsync(conversationId, pageNumber, pageSize);
            var totalCount = await _unitOfWork.ChatMessageRepository.GetTotalMessagesCountAsync(conversationId);

            return new ConversationMessagesResponseDto
            {
                ConversationId = conversationId,
                Messages = messages.Select(m => new MessageResponseDto
                {
                    Id = m.Id,
                    ChatSessionId = m.ChatSessionId,
                    SenderId = m.Sender,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt
                }).ToList(),
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            };
        }

        public async Task<ConversationResponseDto> CreateConversationAsync(int userId, int otherUserId)
        {
            // Check if other user exists
            var otherUser = await _unitOfWork.UserRepository.GetByIdAsync(otherUserId);
            if (otherUser == null)
                throw new NotFoundException("User not found");

            // Check if conversation already exists
            var existingConversation = await _unitOfWork.ChatSessionRepository.FindExistingConversationAsync(userId, otherUserId);
            if (existingConversation != null)
            {
                return new ConversationResponseDto
                {
                    Id = existingConversation.Id,
                    Status = existingConversation.Status,
                    StartedAt = existingConversation.StartedAt,
                    EndedAt = existingConversation.EndedAt,
                    Participants = existingConversation.ChatParticipants.Select(p => new ParticipantResponseDto
                    {
                        UserId = p.UserId,
                        FullName = p.User?.UserProfile?.FullName,
                        Email = p.User?.Email,
                        PhoneNumber = p.User?.PhoneNumber,
                        AvatarUrl = p.User?.AvatarUrl,
                        JoinedAt = p.JoinedAt
                    }).ToList()
                };
            }

            // Create new conversation
            var newConversation = await _unitOfWork.ChatSessionRepository.CreateConversationAsync(userId, otherUserId);

            return new ConversationResponseDto
            {
                Id = newConversation!.Id,
                Status = newConversation.Status,
                StartedAt = newConversation.StartedAt,
                EndedAt = newConversation.EndedAt,
                Participants = newConversation.ChatParticipants.Select(p => new ParticipantResponseDto
                {
                    UserId = p.UserId,
                    FullName = p.User?.UserProfile?.FullName,
                    Email = p.User?.Email,
                    PhoneNumber = p.User?.PhoneNumber,
                    AvatarUrl = p.User?.AvatarUrl,
                    JoinedAt = p.JoinedAt
                }).ToList()
            };
        }
    }
}
