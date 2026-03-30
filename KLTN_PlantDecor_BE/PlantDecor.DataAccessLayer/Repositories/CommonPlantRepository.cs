using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;
using Microsoft.EntityFrameworkCore;

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
                .Where(cp => cp.NurseryId == nurseryId && cp.IsActive && cp.Quantity > 0)
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
                .Where(cp => cp.PlantId == plantId && cp.IsActive && cp.Quantity > 0)
                .Include(cp => cp.Nursery)
                .ToListAsync();
        }

        public async Task<PaginatedResult<CommonPlant>> SearchForShopAsync(
            Pagination pagination,
            string? searchTerm,
            List<int>? categoryIds,
            List<int>? tagIds,
            List<int>? sizes,
            double? minPrice,
            double? maxPrice,
            string? sortBy,
            bool isAscending)
        {
            var query = _context.CommonPlants
                .Include(cp => cp.Plant).ThenInclude(p => p.Categories)
                .Include(cp => cp.Plant).ThenInclude(p => p.Tags)
                .Include(cp => cp.Nursery)
                .Where(cp => cp.IsActive && cp.Quantity > 0 && cp.Nursery.IsActive == true);

            // Search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(cp =>
                    (cp.Plant.Name != null && cp.Plant.Name.ToLower().Contains(term)) ||
                    (cp.Plant.Description != null && cp.Plant.Description.ToLower().Contains(term)) ||
                    (cp.Nursery.Name != null && cp.Nursery.Name.ToLower().Contains(term)));
            }

            // Category filter
            if (categoryIds != null && categoryIds.Any())
            {
                query = query.Where(cp => cp.Plant.Categories.Any(c => categoryIds.Contains(c.Id)));
            }

            // Tag filter
            if (tagIds != null && tagIds.Any())
            {
                query = query.Where(cp => cp.Plant.Tags.Any(t => tagIds.Contains(t.Id)));
            }

            // Size filter
            if (sizes != null && sizes.Any())
            {
                query = query.Where(cp => cp.Plant.Size.HasValue && sizes.Contains(cp.Plant.Size.Value));
            }

            // Price filter
            if (minPrice.HasValue)
            {
                query = query.Where(cp => (double?)cp.Plant.BasePrice >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(cp => (double?)cp.Plant.BasePrice <= maxPrice.Value);
            }

            // Sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "price":
                        query = isAscending
                            ? query.OrderBy(cp => cp.Plant.BasePrice)
                            : query.OrderByDescending(cp => cp.Plant.BasePrice);
                        break;
                    case "name":
                        query = isAscending
                            ? query.OrderBy(cp => cp.Plant.Name)
                            : query.OrderByDescending(cp => cp.Plant.Name);
                        break;
                    default:
                        query = query.OrderByDescending(cp => cp.Id);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(cp => cp.Id);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<CommonPlant>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }
    }
}
