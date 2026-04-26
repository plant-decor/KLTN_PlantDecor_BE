using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class AIChatSessionRepository : GenericRepository<AIChatSession>, IAIChatSessionRepository
    {
        public AIChatSessionRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<AIChatSession> CreateSessionAsync(int userId, string? title = null)
        {
            var session = new AIChatSession
            {
                UserId = userId,
                Title = string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
                Status = (int)AIChatSessionStatusEnum.Active,
                StartedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.AIChatSessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }

        public async Task<AIChatSession?> GetByIdAndUserAsync(int sessionId, int userId)
        {
            return await _context.AIChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);
        }

        public async Task<List<AIChatSession>> GetUserSessionsAsync(int userId, int pageNumber = 1, int pageSize = 20)
        {
            return await _context.AIChatSessions
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartedAt)
                .ThenByDescending(s => s.Id)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetUserSessionsCountAsync(int userId)
        {
            return await _context.AIChatSessions
                .CountAsync(s => s.UserId == userId);
        }

        public async Task<bool> CloseSessionAsync(int sessionId, int userId)
        {
            var session = await _context.AIChatSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.UserId == userId);

            if (session == null)
            {
                return false;
            }

            session.Status = (int)AIChatSessionStatusEnum.Closed;
            session.EndedAt = DateTime.UtcNow;
            session.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
