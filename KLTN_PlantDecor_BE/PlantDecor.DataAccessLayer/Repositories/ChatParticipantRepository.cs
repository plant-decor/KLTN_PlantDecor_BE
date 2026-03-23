using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class ChatParticipantRepository : GenericRepository<ChatParticipant>, IChatParticipantRepository
    {
        public ChatParticipantRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<bool> IsParticipantAsync(int userId, int conversationId)
        {
            return await _context.ChatParticipants
                .AnyAsync(cp => cp.UserId == userId && cp.ChatSessionId == conversationId);
        }

        public async Task<List<ChatParticipant>> GetConversationParticipantsAsync(int conversationId)
        {
            return await _context.ChatParticipants
                .Include(cp => cp.User)
                    .ThenInclude(u => u.UserProfile)
                .Where(cp => cp.ChatSessionId == conversationId)
                .ToListAsync();
        }

        public async Task AddParticipantAsync(int userId, int conversationId)
        {
            var participant = new ChatParticipant
            {
                UserId = userId,
                ChatSessionId = conversationId,
                JoinedAt = DateTime.UtcNow
            };

            _context.ChatParticipants.Add(participant);
            await _context.SaveChangesAsync();
        }
    }
}
