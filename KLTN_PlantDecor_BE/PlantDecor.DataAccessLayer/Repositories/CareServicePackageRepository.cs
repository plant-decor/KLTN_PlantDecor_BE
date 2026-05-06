using System;
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
                .Include(p => p.PackagePlantSuitabilities.Where(pps => pps.IsActive))
                    .ThenInclude(pps => pps.Category)
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
                .Include(p => p.PackagePlantSuitabilities.Where(pps => pps.IsActive))
                    .ThenInclude(pps => pps.Category)
                .FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task<CareServicePackage?> GetByIdWithNurseriesAsync(int id)
        {
            return await _context.CareServicePackages
                .Include(p => p.CareServiceSpecializations)
                    .ThenInclude(cs => cs.Specialization)
                .Include(p => p.PackagePlantSuitabilities.Where(pps => pps.IsActive))
                    .ThenInclude(pps => pps.Category)
                .Include(p => p.NurseryCareServices.Where(ncs => ncs.IsActive))
                    .ThenInclude(ncs => ncs.Nursery)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive == true);
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
                .Include(p => p.CareServiceSpecializations)
                    .ThenInclude(cs => cs.Specialization)
                .Include(p => p.PackagePlantSuitabilities.Where(pps => pps.IsActive))
                    .ThenInclude(pps => pps.Category)
                .Include(p => p.NurseryCareServices.Where(ncs => ncs.IsActive))
                    .ThenInclude(ncs => ncs.Nursery)
                .Where(p => p.IsActive == true && p.NurseryCareServices.Any(ncs => ncs.IsActive))
                .OrderBy(p => p.Id)
                .ToListAsync();
        }

        public async Task<List<CareServicePackage>> GetNotActivelyOfferedByNurseryAsync(int nurseryId)
        {
            return await _context.CareServicePackages
                .Include(p => p.PackagePlantSuitabilities.Where(pps => pps.IsActive))
                    .ThenInclude(pps => pps.Category)
                .Where(p => p.IsActive == true
                    && !p.NurseryCareServices.Any(ncs => ncs.NurseryId == nurseryId && ncs.IsActive))
                .OrderBy(p => p.Id)
                .ToListAsync();
        }

        public async Task<List<PackagePlantSuitability>> GetActiveSuitabilityRulesByPackageIdsAsync(IEnumerable<int> packageIds)
        {
            var ids = packageIds?.Distinct().ToList() ?? new List<int>();
            if (ids.Count == 0)
                return new List<PackagePlantSuitability>();

            return await _context.PackagePlantSuitabilities
                .Include(r => r.Category)
                .Where(r => r.IsActive && ids.Contains(r.CareServicePackageId))
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

        public async Task AddSuitabilityRulesAsync(int packageId, IEnumerable<PackagePlantSuitability> rules)
        {
            var normalizedRules = rules
                .Where(r => r != null)
                .Select(r => new PackagePlantSuitability
                {
                    CareServicePackageId = packageId,
                    CategoryId = r.CategoryId,
                    CareDifficultyLevel = r.CareDifficultyLevel,
                    IsActive = r.IsActive,
                    CreatedAt = r.CreatedAt
                })
                .ToList();

            if (normalizedRules.Count == 0)
                return;

            _context.PackagePlantSuitabilities.AddRange(normalizedRules);
            await _context.SaveChangesAsync();
        }

        public async Task ReplaceSuitabilityRulesAsync(int packageId, IEnumerable<PackagePlantSuitability> rules)
        {
            var existingRules = await _context.PackagePlantSuitabilities
                .Where(r => r.CareServicePackageId == packageId)
                .ToListAsync();

            if (existingRules.Count > 0)
                _context.PackagePlantSuitabilities.RemoveRange(existingRules);

            await AddSuitabilityRulesAsync(packageId, rules);
        }

        public async Task<int> CountForEmbeddingBackfillAsync()
        {
            return await _context.CareServicePackages.CountAsync();
        }

        public async Task<List<CareServicePackage>> GetEmbeddingBackfillBatchAsync(int skip, int take)
        {
            var normalizedSkip = Math.Max(0, skip);
            var normalizedTake = Math.Clamp(take, 1, 500);

            return await _context.CareServicePackages
                .Include(p => p.PackagePlantSuitabilities.Where(r => r.IsActive))
                    .ThenInclude(r => r.Category)
                .OrderBy(p => p.Id)
                .Skip(normalizedSkip)
                .Take(normalizedTake)
                .ToListAsync();
        }
    }
}
