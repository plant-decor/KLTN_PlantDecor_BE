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
                .Include(p => p.CommonPlants.Where(cp => cp.IsActive
                    && cp.Quantity > cp.ReservedQuantity
                    && cp.Nursery != null
                    && cp.Nursery.IsActive == true))
                .Where(p => p.PlantInstances.Any(i => i.Status == (int)PlantInstanceStatusEnum.Available)
                    || p.CommonPlants.Any(cp => cp.IsActive
                        && cp.Quantity > cp.ReservedQuantity
                        && cp.Nursery != null
                        && cp.Nursery.IsActive == true))
                .OrderByDescending(p => p.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Plant>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<Plant>> SearchAllWithDetailsAsync(PlantSearchFilter filter, Pagination pagination)
        {
            var query = _context.Plants
                .Include(p => p.Categories)
                .Include(p => p.Tags)
                .Include(p => p.PlantImages)
                .Include(p => p.PlantInstances)
                .AsQueryable();

            query = ApplyFilters(query, filter, false);
            query = ApplySorting(query, filter, false);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Plant>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<Plant>> SearchForShopAsync(PlantSearchFilter filter, Pagination pagination)
        {
            var query = _context.Plants
                .Where(p => p.IsActive == true)
                .Include(p => p.Categories)
                .Include(p => p.Tags)
                .Include(p => p.PlantImages)
                .Include(p => p.PlantInstances.Where(i => i.Status == (int)PlantInstanceStatusEnum.Available
                    && (!filter.NurseryId.HasValue || i.CurrentNurseryId == filter.NurseryId.Value)))
                .Include(p => p.CommonPlants.Where(cp => cp.IsActive
                    && cp.Quantity > cp.ReservedQuantity
                    && (!filter.NurseryId.HasValue || cp.NurseryId == filter.NurseryId.Value)
                    && cp.Nursery != null
                    && cp.Nursery.IsActive == true))
                .Where(p => p.PlantInstances.Any(i => i.Status == (int)PlantInstanceStatusEnum.Available
                        && (!filter.NurseryId.HasValue || i.CurrentNurseryId == filter.NurseryId.Value))
                    || p.CommonPlants.Any(cp => cp.IsActive
                        && cp.Quantity > cp.ReservedQuantity
                        && (!filter.NurseryId.HasValue || cp.NurseryId == filter.NurseryId.Value)
                        && cp.Nursery != null
                        && cp.Nursery.IsActive == true))
                .AsQueryable();

            query = ApplyFilters(query, filter, true);
            query = ApplySorting(query, filter, true);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Plant>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        private static IQueryable<Plant> ApplyFilters(IQueryable<Plant> query, PlantSearchFilter filter, bool isShop)
        {
            if (!string.IsNullOrWhiteSpace(filter.Keyword))
            {
                var keyword = filter.Keyword.Trim().ToLower();
                query = query.Where(p =>
                    p.Name.ToLower().Contains(keyword)
                    || (p.SpecificName != null && p.SpecificName.ToLower().Contains(keyword))
                    || (p.Origin != null && p.Origin.ToLower().Contains(keyword))
                    || (p.CareLevel != null && p.CareLevel.ToLower().Contains(keyword))
                );
            }

            if (!isShop && filter.IsActive.HasValue)
                query = query.Where(p => p.IsActive == filter.IsActive.Value);

            if (filter.PlacementType.HasValue)
                query = query.Where(p => p.PlacementType == filter.PlacementType.Value);

            if (!string.IsNullOrWhiteSpace(filter.CareLevel))
            {
                var careLevel = filter.CareLevel.Trim().ToLower();
                query = query.Where(p => p.CareLevel != null && p.CareLevel.ToLower() == careLevel);
            }

            if (filter.Toxicity.HasValue)
                query = query.Where(p => p.Toxicity == filter.Toxicity.Value);

            if (filter.AirPurifying.HasValue)
                query = query.Where(p => p.AirPurifying == filter.AirPurifying.Value);

            if (filter.HasFlower.HasValue)
                query = query.Where(p => p.HasFlower == filter.HasFlower.Value);

            if (filter.PetSafe.HasValue)
                query = query.Where(p => p.PetSafe == filter.PetSafe.Value);

            if (filter.ChildSafe.HasValue)
                query = query.Where(p => p.ChildSafe == filter.ChildSafe.Value);

            if (filter.IsUniqueInstance.HasValue)
                query = query.Where(p => p.IsUniqueInstance == filter.IsUniqueInstance.Value);

            if (filter.MinBasePrice.HasValue)
                query = query.Where(p => p.BasePrice.HasValue && p.BasePrice.Value >= filter.MinBasePrice.Value);

            if (filter.MaxBasePrice.HasValue)
                query = query.Where(p => p.BasePrice.HasValue && p.BasePrice.Value <= filter.MaxBasePrice.Value);

            if (filter.CategoryIds != null && filter.CategoryIds.Count > 0)
                query = query.Where(p => p.Categories.Any(c => filter.CategoryIds.Contains(c.Id)));

            if (filter.TagIds != null && filter.TagIds.Count > 0)
                query = query.Where(p => p.Tags.Any(t => filter.TagIds.Contains(t.Id)));

            return query;
        }

        private static IQueryable<Plant> ApplySorting(IQueryable<Plant> query, PlantSearchFilter filter, bool isShop)
        {
            var sortBy = (filter.SortBy ?? "createdAt").Trim().ToLower();
            var isDesc = string.Equals(filter.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(filter.SortDirection);

            return sortBy switch
            {
                "name" => isDesc ? query.OrderByDescending(p => p.Name) : query.OrderBy(p => p.Name),
                "price" => isDesc ? query.OrderByDescending(p => p.BasePrice) : query.OrderBy(p => p.BasePrice),
                "availableinstances" when isShop => isDesc
                    ? query.OrderByDescending(p =>
                        p.PlantInstances.Count(i => i.Status == (int)PlantInstanceStatusEnum.Available
                            && (!filter.NurseryId.HasValue || i.CurrentNurseryId == filter.NurseryId.Value))
                        + p.CommonPlants
                            .Where(cp => cp.IsActive
                                && cp.Quantity > cp.ReservedQuantity
                                && (!filter.NurseryId.HasValue || cp.NurseryId == filter.NurseryId.Value)
                                && cp.Nursery != null
                                && cp.Nursery.IsActive == true)
                            .Sum(cp => cp.Quantity - cp.ReservedQuantity))
                    : query.OrderBy(p =>
                        p.PlantInstances.Count(i => i.Status == (int)PlantInstanceStatusEnum.Available
                            && (!filter.NurseryId.HasValue || i.CurrentNurseryId == filter.NurseryId.Value))
                        + p.CommonPlants
                            .Where(cp => cp.IsActive
                                && cp.Quantity > cp.ReservedQuantity
                                && (!filter.NurseryId.HasValue || cp.NurseryId == filter.NurseryId.Value)
                                && cp.Nursery != null
                                && cp.Nursery.IsActive == true)
                            .Sum(cp => cp.Quantity - cp.ReservedQuantity)),
                "availableinstances" => isDesc
                    ? query.OrderByDescending(p => p.PlantInstances.Count(i => i.Status == (int)PlantInstanceStatusEnum.Available))
                    : query.OrderBy(p => p.PlantInstances.Count(i => i.Status == (int)PlantInstanceStatusEnum.Available)),
                _ => isDesc ? query.OrderByDescending(p => p.CreatedAt) : query.OrderBy(p => p.CreatedAt)
            };
        }
    }
}
