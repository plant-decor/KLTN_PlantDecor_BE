using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class ShiftRepository : GenericRepository<Shift>, IShiftRepository
    {
        public ShiftRepository(PlantDecorContext context) : base(context) { }

        public async Task<bool> ExistsAsync(int id)
        {
            return await _context.Shifts.AnyAsync(s => s.Id == id);
        }
    }
}
