using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class PlantInventoryRepository : GenericRepository<PlantInventory>, IPlantInventoryRepository
    {
        public PlantInventoryRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<PaginatedResult<PlantInventory>> GetAllWithDetailsAsync(Pagination pagination)
        {
            var query = _context.PlantInventories
                .Include(pi => pi.Plant)
                .Include(pi => pi.Nursery)
                .OrderByDescending(pi => pi.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<PlantInventory>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PlantInventory?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.PlantInventories
                .Include(pi => pi.Plant)
                .Include(pi => pi.Nursery)
                .FirstOrDefaultAsync(pi => pi.Id == id);
        }

        public async Task<PaginatedResult<PlantInventory>> GetByPlantIdAsync(int plantId, Pagination pagination)
        {
            var query = _context.PlantInventories
                .Where(pi => pi.PlantId == plantId)
                .Include(pi => pi.Plant)
                .Include(pi => pi.Nursery)
                .OrderByDescending(pi => pi.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<PlantInventory>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<PlantInventory>> GetByNurseryIdAsync(int nurseryId, Pagination pagination)
        {
            var query = _context.PlantInventories
                .Where(pi => pi.NurseryId == nurseryId)
                .Include(pi => pi.Plant)
                .Include(pi => pi.Nursery)
                .OrderByDescending(pi => pi.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<PlantInventory>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PlantInventory?> GetByPlantAndNurseryAsync(int plantId, int nurseryId)
        {
            return await _context.PlantInventories
                .Include(pi => pi.Plant)
                .Include(pi => pi.Nursery)
                .FirstOrDefaultAsync(pi => pi.PlantId == plantId && pi.NurseryId == nurseryId);
        }

        public async Task<bool> ExistsAsync(int plantId, int nurseryId, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await _context.PlantInventories
                    .AnyAsync(pi => pi.PlantId == plantId && pi.NurseryId == nurseryId && pi.Id != excludeId.Value);
            }
            return await _context.PlantInventories
                .AnyAsync(pi => pi.PlantId == plantId && pi.NurseryId == nurseryId);
        }
    }
}
