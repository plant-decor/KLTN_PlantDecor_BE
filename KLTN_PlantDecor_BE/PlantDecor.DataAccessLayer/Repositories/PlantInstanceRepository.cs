using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class PlantInstanceRepository : GenericRepository<PlantInstance>, IPlantInstanceRepository
    {
        public PlantInstanceRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<PaginatedResult<PlantInstance>> GetAllWithPlantAsync(Pagination pagination)
        {
            var query = _context.PlantInstances
                .Include(i => i.Plant)
                .OrderByDescending(i => i.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<PlantInstance>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<PlantInstance>> GetByPlantIdAsync(int plantId, Pagination pagination)
        {
            var query = _context.PlantInstances
                .Include(i => i.Plant)
                .Where(i => i.PlantId == plantId)
                .OrderByDescending(i => i.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<PlantInstance>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PlantInstance?> GetByIdWithPlantAsync(int id)
        {
            return await _context.PlantInstances
                .Include(i => i.Plant)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<PlantInstance?> GetByIdWithOrdersAsync(int id)
        {
            return await _context.PlantInstances
                .Include(i => i.CartItems)
                .Include(i => i.OrderItems)
                .FirstOrDefaultAsync(i => i.Id == id);
        }
    }
}
