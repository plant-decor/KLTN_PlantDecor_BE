using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class PlantGuideRepository : GenericRepository<PlantGuide>, IPlantGuideRepository
    {
        public PlantGuideRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<PaginatedResult<PlantGuide>> GetAllWithPlantAsync(Pagination pagination)
        {
            var query = _context.PlantGuides
                .Include(pg => pg.Plant)
                .OrderByDescending(pg => pg.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(pg => pg.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<PlantGuide>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PlantGuide?> GetByPlantIdWithPlantAsync(int plantId)
        {
            return await _context.PlantGuides
                .Where(pg => pg.PlantId == plantId)
                .Include(pg => pg.Plant)
                .OrderByDescending(pg => pg.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(pg => pg.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<PlantGuide?> GetByIdWithPlantAsync(int id)
        {
            return await _context.PlantGuides
                .Include(pg => pg.Plant)
                .FirstOrDefaultAsync(pg => pg.Id == id);
        }

        public async Task<bool> ExistsByPlantIdAsync(int plantId, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await _context.PlantGuides
                    .AnyAsync(pg => pg.PlantId == plantId && pg.Id != excludeId.Value);
            }

            return await _context.PlantGuides
                .AnyAsync(pg => pg.PlantId == plantId);
        }
    }
}
