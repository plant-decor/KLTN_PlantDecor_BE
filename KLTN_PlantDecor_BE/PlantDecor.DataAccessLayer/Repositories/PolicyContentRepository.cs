using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class PolicyContentRepository : GenericRepository<PolicyContent>, IPolicyContentRepository
    {
        public PolicyContentRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<List<PolicyContent>> GetAllActiveAsync()
        {
            return await _context.PolicyContents
                .Where(p => p.IsActive == true)
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.Id)
                .ToListAsync();
        }

        public async Task<List<PolicyContent>> GetByCategoryActiveAsync(int category)
        {
            return await _context.PolicyContents
                .Where(p => p.IsActive == true && p.Category == category)
                .OrderBy(p => p.DisplayOrder)
                .ThenBy(p => p.Id)
                .ToListAsync();
        }

        public async Task<List<PolicyContent>> GetAdminListAsync(bool includeInactive = true)
        {
            var query = _context.PolicyContents.AsQueryable();

            if (!includeInactive)
            {
                query = query.Where(p => p.IsActive == true);
            }

            return await query
                .OrderBy(p => p.Category)
                .ThenBy(p => p.DisplayOrder)
                .ThenBy(p => p.Id)
                .ToListAsync();
        }

        public async Task<bool> ExistsByTitleInCategoryAsync(string title, int category, int? excludeId = null)
        {
            var normalizedTitle = title.Trim().ToLowerInvariant();
            var query = _context.PolicyContents
                .Where(p => p.Category == category && p.Title != null && p.Title.ToLower() == normalizedTitle);

            if (excludeId.HasValue)
            {
                query = query.Where(p => p.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}
