using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class MaterialRepository : GenericRepository<Material>, IMaterialRepository
    {
        public MaterialRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<PaginatedResult<Material>> GetAllWithDetailsAsync(Pagination pagination)
        {
            var query = _context.Materials
                .Include(m => m.Categories)
                .Include(m => m.Tags)
                .Include(m => m.MaterialImages)
                .OrderByDescending(m => m.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Material>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<Material>> GetActiveWithDetailsAsync(Pagination pagination)
        {
            var query = _context.Materials
                .Where(m => m.IsActive == true)
                .Include(m => m.Categories)
                .Include(m => m.Tags)
                .Include(m => m.MaterialImages)
                .OrderByDescending(m => m.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Material>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<Material?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Materials
                .Include(m => m.Categories)
                .Include(m => m.Tags)
                .Include(m => m.MaterialImages)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<Material?> GetByIdWithOrdersAsync(int id)
        {
            return await _context.Materials
                .Include(m => m.NurseryMaterials)
                    .ThenInclude(nm => nm.CartItems)
                .Include(m => m.NurseryMaterials)
                .FirstOrDefaultAsync(m => m.Id == id);
        }

        public async Task<bool> ExistsByCodeAsync(string materialCode, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await _context.Materials
                    .AnyAsync(m => m.MaterialCode == materialCode && m.Id != excludeId.Value);
            }
            return await _context.Materials
                .AnyAsync(m => m.MaterialCode == materialCode);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var normalizedName = name.Trim().ToLower();

            if (excludeId.HasValue)
            {
                return await _context.Materials
                    .AnyAsync(m => m.Name != null && m.Name.ToLower() == normalizedName && m.Id != excludeId.Value);
            }

            return await _context.Materials
                .AnyAsync(m => m.Name != null && m.Name.ToLower() == normalizedName);
        }

        public async Task<PaginatedResult<Material>> GetMaterialsForShopAsync(Pagination pagination)
        {
            var query = _context.Materials
                .Where(m => m.IsActive == true)
                .Include(m => m.Categories)
                .Include(m => m.Tags)
                .Include(m => m.MaterialImages)
                .Include(m => m.NurseryMaterials)
                .OrderByDescending(m => m.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Material>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }
    }
}
