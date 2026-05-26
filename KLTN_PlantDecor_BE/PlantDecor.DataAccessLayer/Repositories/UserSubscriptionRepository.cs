using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class UserSubscriptionRepository : GenericRepository<UserSubscription>, IUserSubscriptionRepository
    {
        public UserSubscriptionRepository(PlantDecorContext context) : base(context) { }

        public async Task<IEnumerable<UserSubscription>> GetActiveByUserIdAsync(int userId)
        {
            var now = DateTime.Now;
            return await _context.UserSubscriptions
                .Include(s => s.TierPackage)
                .Where(s => s.UserId == userId && s.IsActive && (s.EndDate == null || s.EndDate >= now))
                .OrderBy(s => s.EndDate)
                .ToListAsync();
        }

        public async Task<int> CountUsedQuotaAsync(int subscriptionId)
        {
            return await _context.UserAIUsages
                .Where(u => u.UserSubscriptionId == subscriptionId && u.IsSuccess && !u.IsRefunded)
                .SumAsync(u => u.RequestCount);
        }

        public async Task DeactivateMonthlyFreeAsync(int userId)
        {
            await _context.UserSubscriptions
                .Where(s => s.UserId == userId && s.IsMonthlyFree && s.IsActive)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsActive, false));
        }

        public async Task<IEnumerable<UserSubscription>> GetAllByUserIdAsync(int userId)
        {
            return await _context.UserSubscriptions
                .Include(s => s.TierPackage)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }
    }
}
