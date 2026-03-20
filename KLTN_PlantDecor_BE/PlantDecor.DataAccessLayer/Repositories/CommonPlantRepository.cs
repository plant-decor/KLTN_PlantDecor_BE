using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class CommonPlantRepository : GenericRepository<CommonPlant>, ICommonPlantRepository
    {
        public CommonPlantRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<PaginatedResult<CommonPlant>> GetAllWithDetailsAsync(Pagination pagination)
        {
            var query = _context.CommonPlants
                .Include(cp => cp.Plant)
                .Include(cp => cp.Nursery)
                .OrderByDescending(cp => cp.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<CommonPlant>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<CommonPlant?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.CommonPlants
                .Include(cp => cp.Plant)
                .Include(cp => cp.Nursery)
                .FirstOrDefaultAsync(cp => cp.Id == id);
        }

        public async Task<PaginatedResult<CommonPlant>> GetByPlantIdAsync(int plantId, Pagination pagination)
        {
            var query = _context.CommonPlants
                .Where(cp => cp.PlantId == plantId)
                .Include(cp => cp.Plant)
                .Include(cp => cp.Nursery)
                .OrderByDescending(cp => cp.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<CommonPlant>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<CommonPlant>> GetByNurseryIdAsync(int nurseryId, Pagination pagination)
        {
            var query = _context.CommonPlants
                .Where(cp => cp.NurseryId == nurseryId)
                .Include(cp => cp.Plant)
                .Include(cp => cp.Nursery)
                .OrderByDescending(cp => cp.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<CommonPlant>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<List<CommonPlant>> GetAllByNurseryIdAsync(int nurseryId)
        {
            return await _context.CommonPlants
                .Where(cp => cp.NurseryId == nurseryId)
                .Include(cp => cp.Plant)
                .Include(cp => cp.Nursery)
                .ToListAsync();
        }

        public async Task<CommonPlant?> GetByPlantAndNurseryAsync(int plantId, int nurseryId)
        {
            return await _context.CommonPlants
                .Include(cp => cp.Plant)
                .Include(cp => cp.Nursery)
                .FirstOrDefaultAsync(cp => cp.PlantId == plantId && cp.NurseryId == nurseryId);
        }

        public async Task<bool> ExistsAsync(int plantId, int nurseryId, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await _context.CommonPlants
                    .AnyAsync(cp => cp.PlantId == plantId && cp.NurseryId == nurseryId && cp.Id != excludeId.Value);
            }
            return await _context.CommonPlants
                .AnyAsync(cp => cp.PlantId == plantId && cp.NurseryId == nurseryId);
        }

        public async Task<PaginatedResult<CommonPlant>> GetActiveByNurseryIdAsync(int nurseryId, Pagination pagination)
        {
            var query = _context.CommonPlants
                .Where(cp => cp.NurseryId == nurseryId && cp.IsActive && cp.Quantity > cp.ReservedQuantity)
                .Include(cp => cp.Plant)
                .Include(cp => cp.Nursery)
                .OrderByDescending(cp => cp.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<CommonPlant>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<List<CommonPlant>> GetActiveByPlantIdAsync(int plantId)
        {
            return await _context.CommonPlants
                .Where(cp => cp.PlantId == plantId && cp.IsActive && cp.Quantity > cp.ReservedQuantity)
                .Include(cp => cp.Nursery)
                .ToListAsync();
        }
    }
}
