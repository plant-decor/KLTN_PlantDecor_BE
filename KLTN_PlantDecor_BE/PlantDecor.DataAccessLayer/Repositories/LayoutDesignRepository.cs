using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class LayoutDesignRepository : GenericRepository<LayoutDesign>, ILayoutDesignRepository
    {
        public LayoutDesignRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<LayoutDesign?> GetGenerationContextByIdAsync(int layoutDesignId)
        {
            return await _context.LayoutDesigns
                .AsNoTracking()
                .Include(layout => layout.RoomImage)
                .Include(layout => layout.LayoutDesignPlants)
                .FirstOrDefaultAsync(layout => layout.Id == layoutDesignId);
        }

        public async Task<int?> GetOwnerIdAsync(int layoutDesignId)
        {
            return await _context.LayoutDesigns
                .AsNoTracking()
                .Where(layout => layout.Id == layoutDesignId)
                .Select(layout => layout.UserId)
                .FirstOrDefaultAsync();
        }
    }
}
