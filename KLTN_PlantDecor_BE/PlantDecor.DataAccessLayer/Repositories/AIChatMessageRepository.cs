using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class AIChatMessageRepository : GenericRepository<AIChatMessage>, IAIChatMessageRepository
    {
        public AIChatMessageRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<AIChatMessage> AddUserMessageAsync(int sessionId, int userId, string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Message content cannot be empty.", nameof(content));
            }

            await EnsureOwnedActiveSessionAsync(sessionId, userId);

            var message = new AIChatMessage
            {
                AIChatSessionId = sessionId,
                Role = (int)AIChatMessageRoleEnum.User,
                Content = content.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.AIChatMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<AIChatMessage> AddAssistantMessageAsync(
            int sessionId,
            int userId,
            string content,
            string? intent = null,
            bool isFallback = false,
            bool isPolicyResponse = false,
            string? suggestedPlants = null,
            string? careTips = null)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                throw new ArgumentException("Assistant message content cannot be empty.", nameof(content));
            }

            await EnsureOwnedActiveSessionAsync(sessionId, userId);

            var message = new AIChatMessage
            {
                AIChatSessionId = sessionId,
                Role = (int)AIChatMessageRoleEnum.Assistant,
                Content = content.Trim(),
                Intent = string.IsNullOrWhiteSpace(intent) ? null : intent.Trim(),
                IsFallback = isFallback,
                IsPolicyResponse = isPolicyResponse,
                SuggestedPlants = string.IsNullOrWhiteSpace(suggestedPlants) ? null : suggestedPlants,
                CareTips = string.IsNullOrWhiteSpace(careTips) ? null : careTips,
                CreatedAt = DateTime.UtcNow
            };

            _context.AIChatMessages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<List<AIChatMessage>> GetSessionMessagesAsync(int sessionId, int userId, int pageNumber = 1, int pageSize = 50)
        {
            await EnsureOwnedSessionAsync(sessionId, userId);

            return await _context.AIChatMessages
                .Where(m => m.AIChatSessionId == sessionId)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

            public async Task<int> GetSessionMessagesCountAsync(int sessionId, int userId)
            {
                await EnsureOwnedSessionAsync(sessionId, userId);

                return await _context.AIChatMessages
                .CountAsync(m => m.AIChatSessionId == sessionId);
            }

        public async Task<AIChatMessage?> GetLatestMessageAsync(int sessionId, int userId)
        {
            await EnsureOwnedSessionAsync(sessionId, userId);

            return await _context.AIChatMessages
                .Where(m => m.AIChatSessionId == sessionId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();
        }

        private async Task EnsureOwnedSessionAsync(int sessionId, int userId)
        {
            var exists = await _context.AIChatSessions
                .AnyAsync(s => s.Id == sessionId && s.UserId == userId);

            if (!exists)
            {
                throw new UnauthorizedAccessException("Session does not belong to current user.");
            }
        }

        private async Task EnsureOwnedActiveSessionAsync(int sessionId, int userId)
        {
            var session = await _context.AIChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session == null)
            {
                throw new UnauthorizedAccessException("Session does not belong to current user.");
            }

            if (session.Status != (int)AIChatSessionStatusEnum.Active)
            {
                throw new InvalidOperationException("Cannot append messages to a closed session.");
            }
        }
    }
}
