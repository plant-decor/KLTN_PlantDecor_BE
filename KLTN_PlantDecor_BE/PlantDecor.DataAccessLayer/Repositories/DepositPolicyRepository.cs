using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class DepositPolicyRepository : GenericRepository<DepositPolicy>, IDepositPolicyRepository
    {
        public DepositPolicyRepository(PlantDecorContext context) : base(context) { }

        public async Task<List<DepositPolicy>> GetAllOrderedAsync()
        {
            return await _context.DepositPolicies
                .OrderBy(p => p.MinPrice)
                .ThenBy(p => p.MaxPrice)
                .ToListAsync();
        }

        public async Task<DepositPolicy?> GetMatchingActivePolicyByPriceAsync(decimal price)
        {
            return await _context.DepositPolicies
                .Where(p => p.IsActive
                            && p.MinPrice <= price
                            && (!p.MaxPrice.HasValue || price < p.MaxPrice.Value))
                .OrderByDescending(p => p.MinPrice)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> HasOverlappingActiveRangeAsync(decimal minPrice, decimal? maxPrice, int? excludeId = null)
        {
            var candidateMax = maxPrice ?? decimal.MaxValue;

            var query = _context.DepositPolicies
                .Where(p => p.IsActive);

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }

            return await query.AnyAsync(p =>
                minPrice < (p.MaxPrice ?? decimal.MaxValue)
                && p.MinPrice < candidateMax);
        }
    }
}
