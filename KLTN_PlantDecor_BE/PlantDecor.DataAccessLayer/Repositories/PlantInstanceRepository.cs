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
                .Include(pi => pi.Plant)
                .Include(pi => pi.CurrentNursery)
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
                .Include(pi => pi.Plant)
                    .ThenInclude(p => p.PlantImages)
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
                    .ThenInclude(p => p.Categories)
                .Include(pi => pi.Plant!)
                    .ThenInclude(p => p.Tags)
                .Include(pi => pi.CurrentNursery)
                .OrderBy(pi => pi.Id)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }
    }
}
