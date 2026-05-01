using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class NurseryDesignTemplateRepository : GenericRepository<NurseryDesignTemplate>, INurseryDesignTemplateRepository
    {
        public NurseryDesignTemplateRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<List<NurseryDesignTemplate>> GetByNurseryIdAsync(int nurseryId, bool activeOnly = true)
        {
            var query = _context.NurseryDesignTemplates
                .AsNoTracking()
                .Include(x => x.DesignTemplate)
                .Where(x => x.NurseryId == nurseryId);

            if (activeOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            return await query
                .OrderBy(x => x.DesignTemplateId)
                .ToListAsync();
        }

        public async Task<List<NurseryDesignTemplate>> GetByTemplateIdAsync(int designTemplateId, bool activeOnly = true)
        {
            var query = _context.NurseryDesignTemplates
                .AsNoTracking()
                .Include(x => x.Nursery)
                .Where(x => x.DesignTemplateId == designTemplateId);

            if (activeOnly)
            {
                query = query.Where(x => x.IsActive);
            }

            return await query
                .OrderBy(x => x.NurseryId)
                .ToListAsync();
        }

        public async Task<List<int>> GetActiveDesignTemplateIdsAsync()
        {
            return await _context.NurseryDesignTemplates
                .AsNoTracking()
                .Where(x => x.IsActive && x.Nursery.IsActive == true)
                .Select(x => x.DesignTemplateId)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();
        }

        public async Task<bool> ExistsByNurseryAndTemplateAsync(int nurseryId, int designTemplateId, int? excludeId = null)
        {
            var query = _context.NurseryDesignTemplates
                .Where(x => x.NurseryId == nurseryId && x.DesignTemplateId == designTemplateId);

            if (excludeId.HasValue)
            {
                query = query.Where(x => x.Id != excludeId.Value);
            }

            return await query.AnyAsync();
        }
    }
}