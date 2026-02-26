using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class CategoryRepository : GenericRepository<Category>, ICategoryRepository
    {
        public CategoryRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<PaginatedResult<Category>> GetAllWithParentAsync(Pagination pagination)
        {
            var query = _context.Categories
                .AsNoTracking()
                .Include(c => c.ParentCategory)
                .OrderBy(c => c.Name);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Category>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<List<Category>> GetAllActiveWithParentAsync()
        {
            return await _context.Categories
                .Include(c => c.ParentCategory)
                .OrderBy(c => c.Name)
                .Where(c => c.IsActive == true)
                .ToListAsync();
        }

        public async Task<List<Category>> GetRootCategoriesWithChildrenAsync()
        {
            return await _context.Categories
                .AsNoTracking()
                .Where(c => c.ParentCategoryId == null)
                .Include(c => c.InverseParentCategory)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<Category?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Categories
                .Include(c => c.ParentCategory)
                .Include(c => c.InverseParentCategory)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await _context.Categories
                    .AnyAsync(c => c.Name.ToLower() == name.ToLower() && c.Id != excludeId.Value);
            }
            return await _context.Categories
                .AnyAsync(c => c.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> HasChildrenAsync(int id)
        {
            return await _context.Categories
                .AnyAsync(c => c.ParentCategoryId == id);
        }

        public async Task<bool> HasProductsAsync(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Plants)
                .Include(c => c.Inventories)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null) return false;

            return category.Plants.Any() || category.Inventories.Any();
        }

        public Task<List<Category>> GetRootActiveCategoriesWithChildrenAsync()
        {
            return _context.Categories
                .Where(c => c.ParentCategoryId == null && c.IsActive == true)
                .Include(c => c.InverseParentCategory.Where(sub => sub.IsActive == true))
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}
