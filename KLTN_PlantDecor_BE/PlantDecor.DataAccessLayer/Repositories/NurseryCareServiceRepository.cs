using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class NurseryCareServiceRepository : GenericRepository<NurseryCareService>, INurseryCareServiceRepository
    {
        public NurseryCareServiceRepository(PlantDecorContext context) : base(context) { }

        public async Task<NurseryCareService?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.NurseryCareServices
                .Include(ncs => ncs.CareServicePackage)
                .Include(ncs => ncs.Nursery)
                .FirstOrDefaultAsync(ncs => ncs.Id == id);
        }

        public async Task<List<NurseryCareService>> GetByNurseryIdAsync(int nurseryId)
        {
            return await _context.NurseryCareServices
                .Include(ncs => ncs.CareServicePackage)
                .Where(ncs => ncs.NurseryId == nurseryId && ncs.IsActive)
                .OrderBy(ncs => ncs.Id)
                .ToListAsync();
        }

        public async Task<List<NurseryCareService>> GetAllByNurseryIdAsync(int nurseryId)
        {
            return await _context.NurseryCareServices
                .Include(ncs => ncs.CareServicePackage)
                .Where(ncs => ncs.NurseryId == nurseryId)
                .OrderBy(ncs => ncs.Id)
                .ToListAsync();
        }

        public async Task<bool> ExistsByNurseryAndPackageAsync(int nurseryId, int packageId, int? excludeId = null)
        {
            var query = _context.NurseryCareServices
                .Where(ncs => ncs.NurseryId == nurseryId && ncs.CareServicePackageId == packageId);
            if (excludeId.HasValue)
                query = query.Where(ncs => ncs.Id != excludeId.Value);
            return await query.AnyAsync();
        }
    }
}
