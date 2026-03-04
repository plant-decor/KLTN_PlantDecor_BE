using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class PlantRepository : GenericRepository<Plant>, IPlantRepository
    {
        public PlantRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<PaginatedResult<Plant>> GetAllWithDetailsAsync(Pagination pagination)
        {
            var query = _context.Plants
                .Include(p => p.Categories)
                .Include(p => p.Tags)
                .Include(p => p.PlantImages)
                .Include(p => p.PlantInstances)
                .OrderByDescending(p => p.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Plant>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<Plant>> GetActiveWithDetailsAsync(Pagination pagination)
        {
            var query = _context.Plants
                .Where(p => p.IsActive == true)
                .Include(p => p.Categories)
                .Include(p => p.Tags)
                .Include(p => p.PlantImages)
                .Include(p => p.PlantInstances)
                .OrderByDescending(p => p.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Plant>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<Plant?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Plants
                .Include(p => p.Categories)
                .Include(p => p.Tags)
                .Include(p => p.PlantImages)
                .Include(p => p.PlantInstances)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<Plant?> GetByIdWithInstancesAsync(int id)
        {
            return await _context.Plants
                .Include(p => p.PlantInstances)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await _context.Plants
                    .AnyAsync(p => p.Name == name && p.Id != excludeId.Value);
            }
            return await _context.Plants
                .AnyAsync(p => p.Name == name);
        }

        public async Task<PaginatedResult<Plant>> GetPlantsForShopAsync(Pagination pagination)
        {
            var query = _context.Plants
                .Where(p => p.IsActive == true)
                .Include(p => p.Categories)
                .Include(p => p.Tags)
                .Include(p => p.PlantImages)
                .Include(p => p.PlantInstances.Where(i => i.Status == (int)PlantInstanceStatusEnum.Available))
                .Where(p => p.PlantInstances.Any(i => i.Status == (int)PlantInstanceStatusEnum.Available))
                .OrderByDescending(p => p.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Plant>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }
    }
}
