using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class TagRepository : GenericRepository<Tag>, ITagRepository
    {
        public TagRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<bool> ExistsByNameAsync(string tagName, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await _context.Tags
                    .AnyAsync(t => t.TagName.ToLower() == tagName.ToLower() && t.Id != excludeId.Value);
            }
            return await _context.Tags
                .AnyAsync(t => t.TagName.ToLower() == tagName.ToLower());
        }

        public async Task<Tag?> GetByIdWithProductsAsync(int id)
        {
            return await _context.Tags
                .Include(t => t.Plants)
                .Include(t => t.Inventories)
                .Include(t => t.PlantCombos)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<Tag>> GetByIdsAsync(List<int> ids)
        {
            return await _context.Tags
                .Where(t => ids.Contains(t.Id))
                .ToListAsync();
        }
    }
}
