using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class TierPackageRepository : GenericRepository<TierPackage>, ITierPackageRepository
    {
        public TierPackageRepository(PlantDecorContext context) : base(context) { }

        public async Task<IEnumerable<TierPackage>> GetAllActiveAsync()
        {
            return await _context.TierPackages
                .Where(p => p.IsActive)
                .OrderBy(p => p.Price)
                .ToListAsync();
        }

        public async Task<TierPackage?> GetDefaultFreePackageAsync()
        {
            return await _context.TierPackages
                .Where(p => p.IsActive && p.Price == 0)
                .OrderBy(p => p.Id)
                .FirstOrDefaultAsync();
        }

    }
}
