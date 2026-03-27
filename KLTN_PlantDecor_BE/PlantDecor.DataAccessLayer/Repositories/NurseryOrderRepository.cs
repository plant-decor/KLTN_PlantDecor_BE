using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class NurseryOrderRepository : GenericRepository<NurseryOrder>, INurseryOrderRepository
    {
        public NurseryOrderRepository(PlantDecorContext context) : base(context) { }

        public async Task<List<NurseryOrder>> GetByNurseryIdAsync(int nurseryId)
        {
            return await _context.NurseryOrders
                .Where(no => no.NurseryId == nurseryId)
                .ToListAsync();
        }
    }
}
