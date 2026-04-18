using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class DesignTemplateTierRepository : GenericRepository<DesignTemplateTier>, IDesignTemplateTierRepository
    {
        public DesignTemplateTierRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<DesignTemplateTier?> GetByIdWithItemsAsync(int id)
        {
            return await _context.DesignTemplateTiers
                .AsNoTracking()
                .Include(t => t.DesignTemplateTierItems)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<List<DesignTemplateTier>> GetByTemplateIdWithItemsAsync(int designTemplateId, bool activeOnly = true)
        {
            var query = _context.DesignTemplateTiers
                .AsNoTracking()
                .Include(t => t.DesignTemplateTierItems)
                .Where(t => t.DesignTemplateId == designTemplateId);

            if (activeOnly)
            {
                query = query.Where(t => t.IsActive);
            }

            return await query
                .OrderBy(t => t.MinArea)
                .ThenBy(t => t.Id)
                .ToListAsync();
        }
    }
}