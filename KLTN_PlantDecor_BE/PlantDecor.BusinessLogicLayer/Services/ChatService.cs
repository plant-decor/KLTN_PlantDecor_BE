using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
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

        private static readonly TimeZoneInfo _vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        private static DateTime VnNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vnTimeZone);

        public async Task<ChatMessage> SendMessageAsync(int userId, int conversationId, string content)
        {
            var isParticipant = await _unitOfWork.ChatParticipantRepository.IsParticipantAsync(userId, conversationId);
            if (!isParticipant)
                throw new ForbiddenException("You are not a participant of this conversation");

            var conversation = await _unitOfWork.ChatSessionRepository.GetByIdAsync(conversationId);
            if (conversation == null)
                throw new NotFoundException("Conversation not found");

            if (conversation.Status == (int)ConversationStatus.Closed)
                throw new BadRequestException("Cannot send message to a closed conversation");

            var message = new ChatMessage
            {
                ChatSessionId = conversationId,
                Sender = userId,
                Content = content.Trim(),
                CreatedAt = VnNow
            };

            await _unitOfWork.ChatMessageRepository.CreateAsync(message);
            await _unitOfWork.SaveAsync();
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

        public async Task<ConversationResponseDto?> GetConversationDetailsAsync(int userId, int conversationId, int pageNumber = 1, int pageSize = 30)
        {
            // Check if user is participant
            var isParticipant = await _unitOfWork.ChatParticipantRepository.IsParticipantAsync(userId, conversationId);
            if (!isParticipant)
                throw new ForbiddenException("You are not a participant of this conversation");

            var conversation = await _unitOfWork.ChatSessionRepository.GetConversationWithParticipantsAsync(conversationId);
            if (conversation == null)
                throw new NotFoundException("Conversation not found");

            var latestMessage = await _unitOfWork.ChatMessageRepository.GetLatestMessageAsync(conversationId);
            var messages = await _unitOfWork.ChatMessageRepository.GetConversationMessagesAsync(conversationId, pageNumber, pageSize);
            var totalCount = await _unitOfWork.ChatMessageRepository.GetTotalMessagesCountAsync(conversationId);

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
                } : null,
                Messages = messages.Select(m => new MessageResponseDto
                {
                    Id = m.Id,
                    ChatSessionId = m.ChatSessionId,
                    SenderId = m.Sender,
                    Content = m.Content,
                    CreatedAt = m.CreatedAt
                }).ToList(),
                TotalMessages = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
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

       // bắt đầu cuộc trò chuyện thì hệ thống sẽ tìm thằng consultant ít cuộc trò chuyện nhất trong ngày hôm đó rồi add consultant đó vào cuộc trò chuyện
        public async Task<ConversationResponseDto> StartSupportConversationAsync(int customerId, string firstMessage)
        {
            var existingConversation = await _unitOfWork.ChatSessionRepository
                .FindOpenSupportConversationByCustomerAsync(customerId);

            if (existingConversation == null)
            {
                existingConversation = await _unitOfWork.ChatSessionRepository
                    .CreateSupportConversationAsync(customerId);

                var consultantId = await _unitOfWork.ChatSessionRepository
                    .FindLeastBusyConsultantTodayAsync();

                if (consultantId.HasValue)
                {
                    await _unitOfWork.ChatParticipantRepository.CreateAsync(new ChatParticipant
                    {
                        ChatSessionId = existingConversation.Id,
                        UserId = consultantId.Value,
                        JoinedAt = VnNow
                    });

                    existingConversation.Status = (int)ConversationStatus.Active;
                    await _unitOfWork.ChatSessionRepository.UpdateAsync(existingConversation);
                }
            }

            var message = new ChatMessage
            {
                ChatSessionId = existingConversation.Id,
                Sender = customerId,
                Content = firstMessage.Trim(),
                CreatedAt = VnNow
            };

            await _unitOfWork.ChatMessageRepository.CreateAsync(message);
            await _unitOfWork.SaveAsync();

            return await GetConversationDetailsAsync(customerId, existingConversation.Id)
                   ?? throw new NotFoundException("Conversation not found");
        }

        public async Task<List<ConversationResponseDto>> GetWaitingSupportConversationsAsync()
        {
            var conversations = await _unitOfWork.ChatSessionRepository.GetWaitingSupportConversationsAsync();

            var result = new List<ConversationResponseDto>();

            foreach (var conv in conversations)
            {
                var latestMessage = await _unitOfWork.ChatMessageRepository.GetLatestMessageAsync(conv.Id);

                result.Add(new ConversationResponseDto
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

            return result;
        }

        public async Task<List<ConversationResponseDto>> GetMyClaimedSupportConversationsAsync(int consultantId)
        {
            var conversations = await _unitOfWork.ChatSessionRepository.GetMyClaimedSupportConversationsAsync(consultantId);

            var result = new List<ConversationResponseDto>();

            foreach (var conv in conversations)
            {
                var latestMessage = await _unitOfWork.ChatMessageRepository.GetLatestMessageAsync(conv.Id);

                result.Add(new ConversationResponseDto
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

            return result;
        }

        public async Task<bool> ClaimSupportConversationAsync(int consultantId, int conversationId)
        {
            var conversation = await _unitOfWork.ChatSessionRepository.GetConversationWithParticipantsAsync(conversationId);
            if (conversation == null)
                throw new NotFoundException("Conversation not found");

            if (conversation.Status != (int)ConversationStatus.Waiting)
                return false;

            var isAlreadyParticipant = conversation.ChatParticipants.Any(p => p.UserId == consultantId);
            if (!isAlreadyParticipant)
            {
            await _unitOfWork.ChatParticipantRepository.CreateAsync(new ChatParticipant
                {
                    ChatSessionId = conversationId,
                    UserId = consultantId,
                    JoinedAt = VnNow
                });
            }

            conversation.Status = (int)ConversationStatus.Active;

            await _unitOfWork.ChatSessionRepository.UpdateAsync(conversation);
            await _unitOfWork.SaveAsync();

            return true;
        }

        public async Task<ConversationResponseDto?> GetLatestActiveConversationAsync(int customerId)
        {
            var conversation = await _unitOfWork.ChatSessionRepository.GetLatestActiveConversationAsync(customerId);
            if (conversation == null)
                return null;

            var latestMessage = await _unitOfWork.ChatMessageRepository.GetLatestMessageAsync(conversation.Id);

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

        public async Task CloseConversationAsync(int userId, int conversationId)
        {
            var isParticipant = await _unitOfWork.ChatParticipantRepository.IsParticipantAsync(userId, conversationId);
            if (!isParticipant)
                throw new ForbiddenException("You are not a participant of this conversation");

            var conversation = await _unitOfWork.ChatSessionRepository.GetByIdAsync(conversationId);
            if (conversation == null)
                throw new NotFoundException("Conversation not found");

            conversation.Status = (int)ConversationStatus.Closed;
            conversation.EndedAt = VnNow;

            await _unitOfWork.ChatSessionRepository.UpdateAsync(conversation);
            await _unitOfWork.SaveAsync();
        }
    }
}
