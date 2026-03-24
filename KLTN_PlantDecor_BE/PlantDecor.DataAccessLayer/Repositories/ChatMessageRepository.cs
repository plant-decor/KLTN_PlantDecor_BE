using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class ChatMessageRepository : GenericRepository<ChatMessage>, IChatMessageRepository
    {
        public ChatMessageRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<List<ChatMessage>> GetConversationMessagesAsync(int conversationId, int pageNumber = 1, int pageSize = 50)
        {
            return await _context.ChatMessages
                .Where(m => m.ChatSessionId == conversationId)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .OrderBy(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetTotalMessagesCountAsync(int conversationId)
        {
            return await _context.ChatMessages
                .Where(m => m.ChatSessionId == conversationId)
                .CountAsync();
        }

        public async Task<ChatMessage?> GetLatestMessageAsync(int conversationId)
        {
            return await _context.ChatMessages
                .Where(m => m.ChatSessionId == conversationId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
