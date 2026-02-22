using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class PlantInstanceRepository : GenericRepository<PlantInstance>, IPlantInstanceRepository
    {
        public PlantInstanceRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<List<PlantInstance>> GetAllWithPlantAsync()
        {
            return await _context.PlantInstances
                .Include(i => i.Plant)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<PlantInstance>> GetByPlantIdAsync(int plantId)
        {
            return await _context.PlantInstances
                .Include(i => i.Plant)
                .Where(i => i.PlantId == plantId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
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
