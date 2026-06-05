using Microsoft.Extensions.Configuration;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;
using System.Text.Json;
using System.Text;
using System.Linq;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class ChatService : IChatService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly TimeZoneInfo _vnTimeZone;
        private readonly IAzureOpenAIService _azureOpenAIService;
        private readonly IOnlineTracker _onlineTracker;

        public ChatService(IUnitOfWork unitOfWork, IConfiguration configuration, IAzureOpenAIService azureOpenAIService, IOnlineTracker onlineTracker)
        {
            _unitOfWork = unitOfWork;
            _azureOpenAIService = azureOpenAIService;
            _onlineTracker = onlineTracker;
            var timeZoneId = configuration["TimeZoneId"] ?? "SE Asia Standard Time";
            _vnTimeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }

        public async Task<bool> IsParticipantAsync(int userId, int conversationId)
        {
            return await _unitOfWork.ChatParticipantRepository.IsParticipantAsync(userId, conversationId);
        }

        public async Task<List<int>> GetParticipantIdsAsync(int conversationId)
        {
            var participants = await _unitOfWork.ChatParticipantRepository.GetConversationParticipantsAsync(conversationId);
            return participants.Select(p => p.UserId).ToList();
        }

        private DateTime VnNow => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _vnTimeZone);

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
                    Participants = conv.ChatParticipants.Select(ToParticipantDto).ToList(),
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

        public async Task<ConversationSummaryResponseDto> GetConversationSummaryAsync(int userId, int conversationId)
        {
            // Check participant
            var isParticipant = await _unitOfWork.ChatParticipantRepository.IsParticipantAsync(userId, conversationId);
            if (!isParticipant)
                throw new ForbiddenException("You are not a participant of this conversation");

            var conversation = await _unitOfWork.ChatSessionRepository.GetConversationWithParticipantsAsync(conversationId);
            if (conversation == null)
                throw new NotFoundException("Conversation not found");

            // Load a reasonably large window of messages, then select by time-window (3d -> 1w -> 1m)
            var allMessages = await _unitOfWork.ChatMessageRepository.GetConversationMessagesAsync(conversationId, 1, 1000) ?? new List<ChatMessage>();
            var totalMessages = allMessages.Count;

            // Candidate windows in days (3 days, 1 week, 1 month)
            var windowDays = new[] { 3, 7, 30 };
            List<ChatMessage> selectedWindowMessages = new List<ChatMessage>();

            foreach (var days in windowDays)
            {
                var start = VnNow.AddDays(-days);
                var window = allMessages
                    .Where(m => m.CreatedAt.HasValue && m.CreatedAt.Value >= start && !string.IsNullOrWhiteSpace(m.Content))
                    .OrderBy(m => m.CreatedAt)
                    .ToList();

                if (window.Count > 0)
                {
                    selectedWindowMessages = window;
                    break;
                }
            }

            // If no messages in the windows, fall back to the most recent non-empty messages
            if (selectedWindowMessages.Count == 0)
            {
                selectedWindowMessages = allMessages
                    .Where(m => !string.IsNullOrWhiteSpace(m.Content))
                    .OrderBy(m => m.CreatedAt)
                    .ToList();
            }

            // Build participant summaries (use 0 for unknown sender keys)
            var counts = allMessages?
                .GroupBy(m => m.Sender ?? 0)
                .ToDictionary(g => g.Key, g => g.Count())
                ?? new Dictionary<int, int>();

            var participantSummaries = conversation.ChatParticipants.Select(p => new
            {
                UserId = p.UserId,
                Name = p.User?.UserProfile?.FullName ?? p.User?.Email ?? p.UserId.ToString(),
                MessageCount = counts.ContainsKey(p.UserId) ? counts[p.UserId] : 0
            }).ToList();

            var lastMessage = allMessages != null && allMessages.Count > 0 ? allMessages.Last() : null;
            var lastExcerpt = string.Empty;
            if (lastMessage != null && !string.IsNullOrWhiteSpace(lastMessage.Content))
            {
                lastExcerpt = lastMessage.Content.Length > 240 ? lastMessage.Content.Substring(0, 240).Trim() + "..." : lastMessage.Content.Trim();
            }

            // Key points: pick up to 5 longest non-empty messages as representative highlights
            var keyPoints = selectedWindowMessages == null
                ? new List<string>()
                : selectedWindowMessages
                    .Where(m => !string.IsNullOrWhiteSpace(m.Content))
                    .OrderByDescending(m => m.Content!.Length)
                    .Take(5)
                    .Select(m =>
                    {
                        var senderName = conversation.ChatParticipants.FirstOrDefault(p => p.UserId == (m.Sender ?? 0))?.User?.UserProfile?.FullName
                                         ?? (m.Sender?.ToString() ?? "Unknown");
                        var text = m.Content!.Length > 200 ? m.Content!.Substring(0, 200).Trim() + "..." : m.Content!.Trim();
                        return $"{senderName}: {text}";
                    })
                    .ToList();

            // Build a compacted transcript for the AI prompt using token-aware trimming
            var compact = new StringBuilder();

            // Token budget defaults (align with AISearchService defaults)
            const int maxInputTokens = 1200;
            const int reservedOutputTokens = 400;
            var effectiveInputBudget = Math.Max(120, maxInputTokens - Math.Max(0, reservedOutputTokens));

            // Prepare turns (Role + Content) ordered chronologically
            var turns = selectedWindowMessages
                .Where(m => !string.IsNullOrWhiteSpace(m.Content))
                .Select(m => new
                {
                    Role = conversation.ChatParticipants.FirstOrDefault(p => p.UserId == (m.Sender ?? 0))?.User?.UserProfile?.FullName
                               ?? (m.Sender?.ToString() ?? "Unknown"),
                    Content = m.Content!.Trim(),
                    CreatedAt = m.CreatedAt
                })
                .ToList();

            // Select as many recent turns as fit into the token budget (iterate from newest backwards)
            var selectedTurns = new List<(string Role, string Content, DateTime? CreatedAt)>();
            var usedTokens = 0;

            for (var i = turns.Count - 1; i >= 0; i--)
            {
                var t = turns[i];
                if (string.IsNullOrWhiteSpace(t.Content)) continue;

                var available = effectiveInputBudget - usedTokens;
                if (available <= 8) break;

                var normalized = t.Content.Trim();
                var turnTokens = EstimateTokenCount(normalized) + 4;

                if (turnTokens > available)
                {
                    var truncBudget = Math.Max(8, available - 4);
                    normalized = TruncateByApproxTokens(normalized, truncBudget);
                    turnTokens = EstimateTokenCount(normalized) + 4;
                }

                if (string.IsNullOrWhiteSpace(normalized) || turnTokens > available) continue;

                selectedTurns.Add((t.Role, normalized, t.CreatedAt));
                usedTokens += turnTokens;
            }

            selectedTurns.Reverse();

            foreach (var t in selectedTurns)
            {
                var ts = t.CreatedAt?.ToString("yyyy-MM-dd HH:mm") ?? string.Empty;
                var content = t.Content.Length > 800 ? t.Content.Substring(0, 800).Trim() + "..." : t.Content;
                compact.AppendLine($"[{ts}] {t.Role}: {content}");
            }

            var systemPrompt = "You are a concise support summary assistant for consultants. Return valid JSON only with keys: summary (string), keyPoints (array of strings), nextActions (array of strings). Prefer Vietnamese when the conversation is Vietnamese. Do not include any extra text outside the JSON.";

            var userMessage = new StringBuilder();
            userMessage.AppendLine($"ConversationId: {conversationId}");
            userMessage.AppendLine($"Participants: {string.Join(", ", participantSummaries.Select(s => $"{s.Name}({s.MessageCount})"))}");
            userMessage.AppendLine($"LastMessage: {lastExcerpt}");
            userMessage.AppendLine("Transcript:");
            userMessage.AppendLine(compact.ToString());

            // Try AI summarization, fallback to deterministic summary on any failure
            try
            {
                var aiResp = await _azureOpenAIService.GenerateJsonResponseAsync(systemPrompt, userMessage.ToString());
                if (!string.IsNullOrWhiteSpace(aiResp))
                {
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var aiDto = JsonSerializer.Deserialize<AiSummaryDto>(aiResp, opts);
                    if (aiDto != null && !string.IsNullOrWhiteSpace(aiDto.Summary))
                    {
                        var respDto = new ConversationSummaryResponseDto
                        {
                            ConversationId = conversationId,
                            Summary = aiDto.Summary.Trim(),
                            KeyPoints = aiDto.KeyPoints ?? keyPoints,
                            NextActions = aiDto.NextActions ?? new List<string>(),
                            GeneratedAt = DateTime.UtcNow
                        };

                        // Persist a compact snapshot for ranking/reason if repository available
                        try
                        {
                            var latestCreated = selectedWindowMessages.LastOrDefault()?.CreatedAt;
                            var sourceWindow = "all";
                            if (latestCreated.HasValue)
                            {
                                if (latestCreated.Value >= VnNow.AddDays(-3)) sourceWindow = "3d";
                                else if (latestCreated.Value >= VnNow.AddDays(-7)) sourceWindow = "7d";
                                else sourceWindow = "30d";
                            }

                            // compute transcript hash
                            string transcriptText = compact.ToString();
                            string transcriptHash;
                            using (var sha = System.Security.Cryptography.SHA256.Create())
                            {
                                var bytes = System.Text.Encoding.UTF8.GetBytes(transcriptText);
                                var hash = sha.ComputeHash(bytes);
                                transcriptHash = string.Concat(hash.Select(b => b.ToString("x2")));
                            }

                            var snapshot = new PlantDecor.DataAccessLayer.Entities.ConversationSummarySnapshot
                            {
                                ConversationId = conversationId,
                                Summary = respDto.Summary,
                                KeyPointsJson = JsonSerializer.Serialize(respDto.KeyPoints ?? new List<string>()),
                                NextActionsJson = JsonSerializer.Serialize(respDto.NextActions ?? new List<string>()),
                                StructuredFeaturesJson = null,
                                SourceWindow = sourceWindow,
                                TranscriptHash = transcriptHash,
                                GeneratedAt = DateTime.UtcNow
                            };

                            await _unitOfWork.ConversationSummaryRepository.UpsertAsync(snapshot);
                        }
                        catch
                        {
                            // swallow persistence errors — do not break API response
                        }

                        return respDto;
                    }
                }
            }
            catch (Exception)
            {
                // swallow and fallback
            }

            // Deterministic fallback
            var summaryText = $"Conversation {conversationId}: {totalMessages} messages. Participants: {string.Join(", ", participantSummaries.Select(s => $"{s.Name}({s.MessageCount})"))}. Last message: {lastExcerpt}";

            return new ConversationSummaryResponseDto
            {
                ConversationId = conversationId,
                Summary = summaryText,
                KeyPoints = keyPoints,
                NextActions = new List<string>(),
                GeneratedAt = DateTime.UtcNow
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
                    Participants = existingConversation.ChatParticipants.Select(ToParticipantDto).ToList()
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
                    Participants = conv.ChatParticipants.Select(ToParticipantDto).ToList(),
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
                    Participants = conv.ChatParticipants.Select(ToParticipantDto).ToList(),
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

        public async Task<ConversationResponseDto> GetLatestActiveConversationAsync(int customerId)
        {
            var conversation = await _unitOfWork.ChatSessionRepository.GetLatestActiveConversationAsync(customerId);
            if (conversation == null)
                throw new NotFoundException("No active conversation found");

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

        public async Task<bool> IsConsultantAsync(int userId)
        {
            var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
            return user?.RoleId == (int)RoleEnum.Consultant;
        }

        public async Task<List<int>> GetActiveConversationIdsForConsultantAsync(int consultantId)
        {
            return await _unitOfWork.ChatSessionRepository.GetActiveConversationIdsForConsultantAsync(consultantId);
        }

        private ParticipantResponseDto ToParticipantDto(ChatParticipant p) => new()
        {
            UserId = p.UserId,
            FullName = p.User?.UserProfile?.FullName,
            Email = p.User?.Email,
            PhoneNumber = p.User?.PhoneNumber,
            AvatarUrl = p.User?.AvatarUrl,
            JoinedAt = p.JoinedAt,
            IsOnline = _onlineTracker.IsOnline(p.UserId)
        };

        private static int EstimateTokenCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return 0;

            return Math.Max(1, (int)Math.Ceiling(text.Length / 4.0));
        }

        private static string TruncateByApproxTokens(string text, int tokenBudget)
        {
            if (string.IsNullOrWhiteSpace(text) || tokenBudget <= 0)
                return string.Empty;

            var maxChars = Math.Max(1, tokenBudget * 4);
            if (text.Length <= maxChars)
                return text;

            return text[..maxChars].Trim();
        }

        private class AiSummaryDto
        {
            public string? Summary { get; set; }
            public List<string>? KeyPoints { get; set; }
            public List<string>? NextActions { get; set; }
        }
    }
}
