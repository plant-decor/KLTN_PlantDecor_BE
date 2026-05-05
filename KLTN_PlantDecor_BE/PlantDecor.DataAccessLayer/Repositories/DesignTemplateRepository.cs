using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class DesignTemplateRepository : GenericRepository<DesignTemplate>, IDesignTemplateRepository
    {
        public DesignTemplateRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<DesignTemplate?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.DesignTemplates
                .AsNoTracking()
                .Include(t => t.DesignTemplateTiers)
                    .ThenInclude(tier => tier.DesignTemplateTierItems)
                        .ThenInclude(item => item.Material)
                .Include(t => t.DesignTemplateTiers)
                    .ThenInclude(tier => tier.DesignTemplateTierItems)
                        .ThenInclude(item => item.Plant)
                .Include(t => t.DesignTemplateSpecializations)
                    .ThenInclude(ts => ts.Specialization)
                .Include(t => t.NurseryDesignTemplates)
                    .ThenInclude(ndt => ndt.Nursery)
                .FirstOrDefaultAsync(t => t.Id == id);
        }
    }
}