using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class NurseryMaterialRepository : GenericRepository<NurseryMaterial>, INurseryMaterialRepository
    {
        public NurseryMaterialRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<PaginatedResult<NurseryMaterial>> GetAllWithDetailsAsync(Pagination pagination)
        {
            var query = _context.NurseryMaterials
                .Include(nm => nm.Material)
                .Include(nm => nm.Nursery)
                .OrderByDescending(nm => nm.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<NurseryMaterial>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<NurseryMaterial?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.NurseryMaterials
                .Include(nm => nm.Material)
                .Include(nm => nm.Nursery)
                .FirstOrDefaultAsync(nm => nm.Id == id);
        }

        public async Task<PaginatedResult<NurseryMaterial>> GetByNurseryIdAsync(int nurseryId, Pagination pagination)
        {
            var query = _context.NurseryMaterials
                .Where(nm => nm.NurseryId == nurseryId)
                .Include(nm => nm.Material)
                .Include(nm => nm.Nursery)
                .OrderByDescending(nm => nm.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<NurseryMaterial>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<List<NurseryMaterial>> GetAllByNurseryIdAsync(int nurseryId)
        {
            return await _context.NurseryMaterials
                .Where(nm => nm.NurseryId == nurseryId)
                .Include(nm => nm.Material)
                .Include(nm => nm.Nursery)
                .ToListAsync();
        }

        public async Task<PaginatedResult<NurseryMaterial>> GetByMaterialIdAsync(int materialId, Pagination pagination)
        {
            var query = _context.NurseryMaterials
                .Where(nm => nm.MaterialId == materialId)
                .Include(nm => nm.Material)
                .Include(nm => nm.Nursery)
                .OrderByDescending(nm => nm.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<NurseryMaterial>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<NurseryMaterial?> GetByMaterialAndNurseryAsync(int materialId, int nurseryId)
        {
            return await _context.NurseryMaterials
                .Include(nm => nm.Material)
                .Include(nm => nm.Nursery)
                .FirstOrDefaultAsync(nm => nm.MaterialId == materialId && nm.NurseryId == nurseryId);
        }

        public async Task<bool> ExistsAsync(int materialId, int nurseryId, int? excludeId = null)
        {
            var query = _context.NurseryMaterials
                .Where(nm => nm.MaterialId == materialId && nm.NurseryId == nurseryId);
            if (excludeId.HasValue)
                query = query.Where(nm => nm.Id != excludeId.Value);
            return await query.AnyAsync();
        }

        public async Task<PaginatedResult<NurseryMaterial>> SearchForShopAsync(
            Pagination pagination,
            string? searchTerm,
            List<int>? categoryIds,
            List<int>? tagIds,
            double? minPrice,
            double? maxPrice,
            string? sortBy,
            bool isAscending)
        {
            var query = _context.NurseryMaterials
                .Include(nm => nm.Material).ThenInclude(m => m.Categories)
                .Include(nm => nm.Material).ThenInclude(m => m.Tags)
                .Include(nm => nm.Nursery)
                .Where(nm => nm.IsActive && nm.Quantity > nm.ReservedQuantity && nm.Nursery.IsActive == true);

            // Search term
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(nm =>
                    (nm.Material.Name != null && nm.Material.Name.ToLower().Contains(term)) ||
                    (nm.Material.Description != null && nm.Material.Description.ToLower().Contains(term)) ||
                    (nm.Nursery.Name != null && nm.Nursery.Name.ToLower().Contains(term)));
            }

            // Category filter
            if (categoryIds != null && categoryIds.Any())
            {
                query = query.Where(nm => nm.Material.Categories.Any(c => categoryIds.Contains(c.Id)));
            }

            // Tag filter
            if (tagIds != null && tagIds.Any())
            {
                query = query.Where(nm => nm.Material.Tags.Any(t => tagIds.Contains(t.Id)));
            }

            // Price filter
            if (minPrice.HasValue)
            {
                query = query.Where(nm => (double?)nm.Material.BasePrice >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(nm => (double?)nm.Material.BasePrice <= maxPrice.Value);
            }

            // Sorting
            if (!string.IsNullOrEmpty(sortBy))
            {
                switch (sortBy.ToLower())
                {
                    case "price":
                        query = isAscending
                            ? query.OrderBy(nm => nm.Material.BasePrice)
                            : query.OrderByDescending(nm => nm.Material.BasePrice);
                        break;
                    case "name":
                        query = isAscending
                            ? query.OrderBy(nm => nm.Material.Name)
                            : query.OrderByDescending(nm => nm.Material.Name);
                        break;
                    default:
                        query = query.OrderByDescending(nm => nm.Id);
                        break;
                }
            }
            else
            {
                query = query.OrderByDescending(nm => nm.Id);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<NurseryMaterial>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<int> CountForEmbeddingBackfillAsync()
        {
            return await _context.NurseryMaterials.CountAsync();
        }

        public async Task<List<NurseryMaterial>> GetEmbeddingBackfillBatchAsync(int skip, int take)
        {
            return await _context.NurseryMaterials
                .AsNoTracking()
                .Include(nm => nm.Material)
                    .ThenInclude(m => m.Categories)
                .Include(nm => nm.Material)
                    .ThenInclude(m => m.Tags)
                .Include(nm => nm.Nursery)
                .OrderBy(nm => nm.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }
    }
}
