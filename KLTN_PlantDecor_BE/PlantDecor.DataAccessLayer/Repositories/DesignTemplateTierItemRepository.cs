using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class DesignTemplateTierItemRepository : GenericRepository<DesignTemplateTierItem>, IDesignTemplateTierItemRepository
    {
        public DesignTemplateTierItemRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<List<DesignTemplateTierItem>> GetByTierIdAsync(int designTemplateTierId)
        {
            return await _context.DesignTemplateTierItems
                .AsNoTracking()
                .Include(i => i.Material)
                .Include(i => i.Plant)
                .Where(i => i.DesignTemplateTierId == designTemplateTierId)
                .OrderBy(i => i.Id)
                .ToListAsync();
        }
    }
}