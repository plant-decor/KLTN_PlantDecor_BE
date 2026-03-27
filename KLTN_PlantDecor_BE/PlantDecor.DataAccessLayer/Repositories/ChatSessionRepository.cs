using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class ChatSessionRepository : GenericRepository<ChatSession>, IChatSessionRepository
    {
        public ChatSessionRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<List<ChatSession>> GetUserConversationsAsync(int userId)
        {
            return await _context.ChatSessions
                .Include(cs => cs.ChatParticipants)
                    .ThenInclude(cp => cp.User)
                        .ThenInclude(u => u.UserProfile)
                .Include(cs => cs.ChatMessages.OrderByDescending(m => m.CreatedAt).Take(1))
                .Where(cs => cs.ChatParticipants.Any(cp => cp.UserId == userId))
                .OrderByDescending(cs => cs.ChatMessages.Max(m => m.CreatedAt))
                .ToListAsync();
        }

        public async Task<ChatSession?> GetConversationWithParticipantsAsync(int conversationId)
        {
            return await _context.ChatSessions
                .Include(cs => cs.ChatParticipants)
                    .ThenInclude(cp => cp.User)
                        .ThenInclude(u => u.UserProfile)
                .FirstOrDefaultAsync(cs => cs.Id == conversationId);
        }

        public async Task<ChatSession?> FindExistingConversationAsync(int userId1, int userId2)
        {
            return await _context.ChatSessions
                .Include(cs => cs.ChatParticipants)
                .Where(cs => cs.ChatParticipants.Count == 2 &&
                            cs.ChatParticipants.Any(cp => cp.UserId == userId1) &&
                            cs.ChatParticipants.Any(cp => cp.UserId == userId2))
                .FirstOrDefaultAsync();
        }

        public async Task<ChatSession?> CreateConversationAsync(int userId1, int userId2)
        {
            var chatSession = new ChatSession
            {
                Status = 1,
                StartedAt = DateTime.UtcNow
            };

            _context.ChatSessions.Add(chatSession);
            await _context.SaveChangesAsync();

            var participants = new List<ChatParticipant>
            {
                new ChatParticipant { ChatSessionId = chatSession.Id, UserId = userId1, JoinedAt = DateTime.UtcNow },
                new ChatParticipant { ChatSessionId = chatSession.Id, UserId = userId2, JoinedAt = DateTime.UtcNow }
            };

            _context.ChatParticipants.AddRange(participants);
            await _context.SaveChangesAsync();

            return await GetConversationWithParticipantsAsync(chatSession.Id);
        }
    }
}
