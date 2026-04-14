using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class PlantInstanceRepository : GenericRepository<PlantInstance>, IPlantInstanceRepository
    {
        public PlantInstanceRepository(PlantDecorContext context) : base(context) { }

        public async Task<PaginatedResult<PlantInstance>> GetByNurseryIdAsync(int nurseryId, Pagination pagination, int? statusFilter = null)
        {
            var query = _context.PlantInstances
                .Where(pi => pi.CurrentNurseryId == nurseryId)
                .Include(pi => pi.Plant)
                .Include(pi => pi.PlantImages)
                .Include(pi => pi.CurrentNursery)
                .AsQueryable();

            if (statusFilter.HasValue)
            {
                query = query.Where(pi => pi.Status == statusFilter.Value);
            }

            query = query.OrderByDescending(pi => pi.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<PlantInstance>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PlantInstance?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.PlantInstances
                .Include(pi => pi.Plant!)
                    .ThenInclude(p => p.PlantGuide)
                .Include(pi => pi.CurrentNursery)
                .Include(pi => pi.PlantImages)
                .FirstOrDefaultAsync(pi => pi.Id == id);
        }

        // Dùng cho trường hợp PlantInstance không có ảnh riêng, fallback lấy ảnh từ Plant
        public async Task<string?> GetPrimaryImageUrlAsync(int plantInstanceId)
        {
            var instanceImages = await _context.PlantImages
                .Where(image => image.PlantInstanceId == plantInstanceId)
                .ToListAsync();

            var instanceImageUrl = SelectPrimaryImageUrl(instanceImages);
            if (!string.IsNullOrWhiteSpace(instanceImageUrl))
            {
                return instanceImageUrl;
            }

            var plantId = await _context.PlantInstances
                .Where(pi => pi.Id == plantInstanceId)
                .Select(pi => (int?)pi.PlantId)
                .FirstOrDefaultAsync();

            if (!plantId.HasValue)
            {
                return null;
            }

            var plantImages = await _context.PlantImages
                .Where(image => image.PlantId == plantId.Value && image.PlantInstanceId == null)
                .ToListAsync();

            return SelectPrimaryImageUrl(plantImages);
        }

        public async Task<Dictionary<int, string>> GetPrimaryImageUrlsAsync(IEnumerable<int> plantInstanceIds)
        {
            var normalizedIds = plantInstanceIds
                .Distinct()
                .ToList();

            if (normalizedIds.Count == 0)
            {
                return new Dictionary<int, string>();
            }

            var result = new Dictionary<int, string>();

            var instanceImages = await _context.PlantImages
                .AsNoTracking()
                .Where(image => image.PlantInstanceId.HasValue
                    && normalizedIds.Contains(image.PlantInstanceId.Value)
                    && !string.IsNullOrWhiteSpace(image.ImageUrl))
                .ToListAsync();

            var instanceImageLookup = instanceImages
                .GroupBy(image => image.PlantInstanceId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => SelectPrimaryImageUrl(group.AsEnumerable()));

            foreach (var kvp in instanceImageLookup)
            {
                if (!string.IsNullOrWhiteSpace(kvp.Value))
                {
                    result[kvp.Key] = kvp.Value!;
                }
            }

            var missingIds = normalizedIds
                .Where(id => !result.ContainsKey(id))
                .ToList();

            if (missingIds.Count == 0)
            {
                return result;
            }

            var instanceToPlantMappings = await _context.PlantInstances
                .AsNoTracking()
                .Where(instance => missingIds.Contains(instance.Id) && instance.PlantId.HasValue)
                .Select(instance => new
                {
                    instance.Id,
                    PlantId = instance.PlantId!.Value
                })
                .ToListAsync();

            var plantIds = instanceToPlantMappings
                .Select(mapping => mapping.PlantId)
                .Distinct()
                .ToList();

            if (plantIds.Count == 0)
            {
                return result;
            }

            var plantImages = await _context.PlantImages
                .AsNoTracking()
                .Where(image => image.PlantInstanceId == null
                    && plantIds.Contains(image.PlantId)
                    && !string.IsNullOrWhiteSpace(image.ImageUrl))
                .ToListAsync();

            var plantImageLookup = plantImages
                .GroupBy(image => image.PlantId)
                .ToDictionary(
                    group => group.Key,
                    group => SelectPrimaryImageUrl(group.AsEnumerable()));

            foreach (var mapping in instanceToPlantMappings)
            {
                if (plantImageLookup.TryGetValue(mapping.PlantId, out var plantImageUrl)
                    && !string.IsNullOrWhiteSpace(plantImageUrl))
                {
                    result[mapping.Id] = plantImageUrl!;
                }
            }

            return result;
        }

        public async Task<List<PlantInstance>> GetByIdsAsync(List<int> ids)
        {
            return await _context.PlantInstances
                .Where(pi => ids.Contains(pi.Id))
                .Include(pi => pi.Plant)
                .ToListAsync();
        }

        public async Task<List<PlantInstance>> GetAllByNurseryIdAsync(int nurseryId)
        {
            return await _context.PlantInstances
                .Where(pi => pi.CurrentNurseryId == nurseryId)
                .Include(pi => pi.Plant!)
                    .ThenInclude(p => p.PlantImages)
                .Include(pi => pi.CurrentNursery)
                .ToListAsync();
        }

        public async Task<List<PlantInstance>> GetAvailableByPlantIdAsync(int plantId)
        {
            return await _context.PlantInstances
                .Where(pi => pi.PlantId == plantId && pi.Status == (int)PlantInstanceStatusEnum.Available)
                .Include(pi => pi.CurrentNursery)
                .ToListAsync();
        }

        public async Task AddRangeAsync(IEnumerable<PlantInstance> instances)
        {
            await _context.PlantInstances.AddRangeAsync(instances);
        }

        public async Task<PaginatedResult<PlantInstance>> GetAvailableByNurseryIdAsync(int nurseryId, Pagination pagination, int? plantId = null)
        {
            var query = _context.PlantInstances
                .Where(pi => pi.CurrentNurseryId == nurseryId && pi.Status == (int)PlantInstanceStatusEnum.Available)
                .Include(pi => pi.Plant)
                .Include(pi => pi.PlantImages)
                .Include(pi => pi.CurrentNursery)
                .AsQueryable();

            if (plantId.HasValue)
            {
                query = query.Where(pi => pi.PlantId == plantId.Value);
            }

            query = query.OrderByDescending(pi => pi.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<PlantInstance>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<PlantInstance>> GetAvailableForShopAsync(Pagination pagination, int? nurseryId = null, int? plantId = null)
        {
            var query = _context.PlantInstances
                .Where(pi => pi.Status == (int)PlantInstanceStatusEnum.Available)
                .Include(pi => pi.Plant)
                .Include(pi => pi.PlantImages)
                .Include(pi => pi.CurrentNursery)
                .AsQueryable();

            if (nurseryId.HasValue)
            {
                query = query.Where(pi => pi.CurrentNurseryId == nurseryId.Value);
            }

            if (plantId.HasValue)
            {
                query = query.Where(pi => pi.PlantId == plantId.Value);
            }

            query = query.OrderByDescending(pi => pi.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<PlantInstance>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        private static string? SelectPrimaryImageUrl(IEnumerable<PlantImage>? images)
        {
            if (images == null)
            {
                return null;
            }

            return images
                .Where(image => !string.IsNullOrWhiteSpace(image.ImageUrl))
                .OrderByDescending(image => image.IsPrimary == true)
                .Select(image => image.ImageUrl)
                .FirstOrDefault();
        }

        public async Task<int> CountForEmbeddingBackfillAsync()
        {
            return await _context.PlantInstances.CountAsync();
        }

        public async Task<List<PlantInstance>> GetEmbeddingBackfillBatchAsync(int skip, int take)
        {
            return await _context.PlantInstances
                .AsNoTracking()
                .Include(pi => pi.Plant!)
                    .ThenInclude(p => p.PlantGuide)
                .Include(pi => pi.Plant!)
                    .ThenInclude(p => p.Categories)
                .Include(pi => pi.Plant!)
                    .ThenInclude(p => p.Tags)
                .Include(pi => pi.CurrentNursery)
                .OrderBy(pi => pi.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<List<PlantInstance>> GetByPlantIdForEmbeddingAsync(int plantId)
        {
            return await _context.PlantInstances
                .AsNoTracking()
                .Where(pi => pi.PlantId == plantId)
                .Include(pi => pi.Plant!)
                    .ThenInclude(p => p.PlantGuide)
                .Include(pi => pi.Plant!)
                    .ThenInclude(p => p.Categories)
                .Include(pi => pi.Plant!)
                    .ThenInclude(p => p.Tags)
                .Include(pi => pi.CurrentNursery)
                .OrderBy(pi => pi.Id)
                .ToListAsync();
        }
    }
}
