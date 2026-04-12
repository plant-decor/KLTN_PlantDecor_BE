using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class SpecializationRepository : GenericRepository<Specialization>, ISpecializationRepository
    {
        public SpecializationRepository(PlantDecorContext context) : base(context) { }

        public async Task<List<Specialization>> GetAllActiveAsync()
        {
            return await _context.Specializations
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Specialization?> GetByIdWithRelationsAsync(int id)
        {
            return await _context.Specializations
                .Include(s => s.StaffSpecializations)
                    .ThenInclude(ss => ss.Staff)
                .Include(s => s.CareServiceSpecializations)
                    .ThenInclude(cs => cs.CareServicePackage)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var query = _context.Specializations.Where(s => s.Name == name);
            if (excludeId.HasValue)
                query = query.Where(s => s.Id != excludeId.Value);
            return await query.AnyAsync();
        }

        public async Task<StaffSpecialization?> GetStaffSpecializationAsync(int staffId, int specializationId)
        {
            return await _context.StaffSpecializations
                .FirstOrDefaultAsync(ss => ss.StaffId == staffId && ss.SpecializationId == specializationId);
        }

        public async Task AddStaffSpecializationAsync(StaffSpecialization assignment)
        {
            _context.StaffSpecializations.Add(assignment);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveStaffSpecializationAsync(StaffSpecialization existing)
        {
            _context.StaffSpecializations.Remove(existing);
            await _context.SaveChangesAsync();
        }

        public async Task<List<StaffSpecialization>> GetStaffSpecializationsAsync(int staffId)
        {
            return await _context.StaffSpecializations
                .Where(ss => ss.StaffId == staffId)
                .ToListAsync();
        }

        public async Task ReplaceStaffSpecializationsAsync(int staffId, List<int> specializationIds)
        {
            var existing = await _context.StaffSpecializations
                .Where(ss => ss.StaffId == staffId)
                .ToListAsync();

            _context.StaffSpecializations.RemoveRange(existing);

            var newEntries = specializationIds
                .Distinct()
                .Select(sid => new StaffSpecialization
                {
                    StaffId = staffId,
                    SpecializationId = sid
                }).ToList();

            if (newEntries.Count > 0)
                _context.StaffSpecializations.AddRange(newEntries);

            await _context.SaveChangesAsync();
        }
    }
}
