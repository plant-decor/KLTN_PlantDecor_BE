using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class DesignTemplateSpecializationRepository : GenericRepository<DesignTemplateSpecialization>, IDesignTemplateSpecializationRepository
    {
        public DesignTemplateSpecializationRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<List<DesignTemplateSpecialization>> GetByTemplateIdAsync(int designTemplateId)
        {
            return await _context.DesignTemplateSpecializations
                .AsNoTracking()
                .Include(x => x.Specialization)
                .Where(x => x.DesignTemplateId == designTemplateId)
                .OrderBy(x => x.SpecializationId)
                .ToListAsync();
        }

        public async Task<List<DesignTemplateSpecialization>> GetBySpecializationIdAsync(int specializationId)
        {
            return await _context.DesignTemplateSpecializations
                .AsNoTracking()
                .Include(x => x.DesignTemplate)
                .Where(x => x.SpecializationId == specializationId)
                .OrderBy(x => x.DesignTemplateId)
                .ToListAsync();
        }

        public async Task<bool> ExistsAsync(int designTemplateId, int specializationId)
        {
            return await _context.DesignTemplateSpecializations
                .AnyAsync(x => x.DesignTemplateId == designTemplateId && x.SpecializationId == specializationId);
        }
    }
}