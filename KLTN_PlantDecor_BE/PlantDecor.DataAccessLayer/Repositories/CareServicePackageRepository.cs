using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class CareServicePackageRepository : GenericRepository<CareServicePackage>, ICareServicePackageRepository
    {
        public CareServicePackageRepository(PlantDecorContext context) : base(context) { }

        public async Task<List<CareServicePackage>> GetAllActiveAsync()
        {
            return await _context.CareServicePackages
                .Include(p => p.CareServiceSpecializations)
                    .ThenInclude(cs => cs.Specialization)
                .Where(p => p.IsActive == true
                    && p.NurseryCareServices.Any(ncs => ncs.IsActive))
                .OrderBy(p => p.Id)
                .ToListAsync();
        }

        public async Task<CareServicePackage?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.CareServicePackages
                .Include(p => p.CareServiceSpecializations)
                    .ThenInclude(cs => cs.Specialization)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var query = _context.CareServicePackages.Where(p => p.Name == name);
            if (excludeId.HasValue)
                query = query.Where(p => p.Id != excludeId.Value);
            return await query.AnyAsync();
        }

        public async Task<List<CareServicePackage>> GetPackagesWithNurseriesAsync()
        {
            return await _context.CareServicePackages
                .Where(p => p.IsActive == true && p.NurseryCareServices.Any())
                .OrderBy(p => p.Id)
                .ToListAsync();
        }

        public async Task<List<CareServicePackage>> GetNotActivelyOfferedByNurseryAsync(int nurseryId)
        {
            return await _context.CareServicePackages
                .Where(p => p.IsActive == true
                    && !p.NurseryCareServices.Any(ncs => ncs.NurseryId == nurseryId && ncs.IsActive))
                .OrderBy(p => p.Id)
                .ToListAsync();
        }

        public async Task AddSpecializationsAsync(int packageId, List<int> specializationIds)
        {
            var existing = await _context.CareServiceSpecializations
                .Where(cs => cs.PackageId == packageId && specializationIds.Contains(cs.SpecializationId))
                .Select(cs => cs.SpecializationId)
                .ToListAsync();

            var newEntries = specializationIds
                .Except(existing)
                .Select(sid => new CareServiceSpecialization
                {
                    PackageId = packageId,
                    SpecializationId = sid
                }).ToList();

            if (newEntries.Count > 0)
            {
                _context.CareServiceSpecializations.AddRange(newEntries);
                await _context.SaveChangesAsync();
            }
        }

        public async Task ReplaceSpecializationsAsync(int packageId, List<int> specializationIds)
        {
            var existing = await _context.CareServiceSpecializations
                .Where(cs => cs.PackageId == packageId)
                .ToListAsync();

            _context.CareServiceSpecializations.RemoveRange(existing);

            var newEntries = specializationIds
                .Distinct()
                .Select(sid => new CareServiceSpecialization
                {
                    PackageId = packageId,
                    SpecializationId = sid
                }).ToList();

            if (newEntries.Count > 0)
                _context.CareServiceSpecializations.AddRange(newEntries);

            await _context.SaveChangesAsync();
        }
    }
}
