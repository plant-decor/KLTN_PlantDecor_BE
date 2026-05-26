using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class TierThresholdRepository : GenericRepository<TierThreshold>, ITierThresholdRepository
    {
        public TierThresholdRepository(PlantDecorContext context) : base(context) { }

        public async Task<IEnumerable<TierThreshold>> GetAllActiveAsync()
        {
            return await _context.TierThresholds
                .Where(t => t.IsActive)
                .OrderByDescending(t => t.TierLevel)
                .ToListAsync();
        }

        public async Task<TierThreshold?> GetByTierLevelAsync(int tierLevel)
        {
            return await _context.TierThresholds
                .FirstOrDefaultAsync(t => t.IsActive && t.TierLevel == tierLevel);
        }

    }
}
