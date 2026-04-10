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
    }
}
