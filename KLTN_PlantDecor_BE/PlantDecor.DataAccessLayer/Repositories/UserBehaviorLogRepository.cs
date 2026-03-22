using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class UserBehaviorLogRepository : GenericRepository<UserBehaviorLog>, IUserBehaviorLogRepository
    {

        public UserBehaviorLogRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task AddLogAsync(UserBehaviorLog log)
        {
            await _context.UserBehaviorLogs.AddAsync(log);
        }

        public async Task<List<UserBehaviorLog>> GetLogsByUserAndDateAsync(int userId, DateTime fromDate)
        {
            return await _context.UserBehaviorLogs
                .Where(b => b.UserId == userId && b.CreatedAt >= fromDate)
                .ToListAsync();
        }
    }
}
