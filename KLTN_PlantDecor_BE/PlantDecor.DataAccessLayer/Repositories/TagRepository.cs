using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class TagRepository : GenericRepository<Tag>, ITagRepository
    {
        public TagRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<PaginatedResult<Tag>> GetAllTagsWithPaginationAsync(Pagination pagination)
        {
            var query = _context.Tags.OrderBy(t => t.TagName);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Tag>(items, totalCount, pagination.PageNumber, pagination.PageSize);
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
                .Include(t => t.Materials)
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
