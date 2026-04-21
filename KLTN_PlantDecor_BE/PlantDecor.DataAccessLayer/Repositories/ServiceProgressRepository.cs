using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class ServiceProgressRepository : GenericRepository<ServiceProgress>, IServiceProgressRepository
    {
        public ServiceProgressRepository(PlantDecorContext context) : base(context) { }

        public async Task<List<ServiceProgress>> GetByServiceRegistrationIdAsync(int serviceRegistrationId)
        {
            return await _context.ServiceProgresses
                .Include(sp => sp.Shift)
                .Include(sp => sp.Caretaker)
                .Where(sp => sp.ServiceRegistrationId == serviceRegistrationId)
                .OrderBy(sp => sp.TaskDate)
                .ToListAsync();
        }

        public async Task<List<ServiceProgress>> GetByCaretakerAndDateAsync(int caretakerId, DateOnly date)
        {
            return await _context.ServiceProgresses
                .Include(sp => sp.Shift)
                .Include(sp => sp.ServiceRegistration)
                    .ThenInclude(r => r!.NurseryCareService)
                        .ThenInclude(ncs => ncs!.CareServicePackage)
                .Include(sp => sp.ServiceRegistration)
                    .ThenInclude(r => r!.NurseryCareService)
                        .ThenInclude(ncs => ncs!.Nursery)
                .Where(sp => sp.CaretakerId == caretakerId && sp.TaskDate == date)
                .OrderBy(sp => sp.Shift.StartTime)
                .ToListAsync();
        }

        public async Task<ServiceProgress?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.ServiceProgresses
                .Include(sp => sp.Shift)
                .Include(sp => sp.Caretaker)
                .Include(sp => sp.ServiceRegistration)
                    .ThenInclude(r => r!.NurseryCareService)
                        .ThenInclude(ncs => ncs!.CareServicePackage)
                .Include(sp => sp.ServiceRegistration)
                    .ThenInclude(r => r!.NurseryCareService)
                        .ThenInclude(ncs => ncs!.Nursery)
                .Include(sp => sp.ServiceRegistration)
                    .ThenInclude(r => r!.User)
                .FirstOrDefaultAsync(sp => sp.Id == id);
        }

        public async Task<List<ServiceProgress>> GetByNurseryAndDateAsync(int nurseryId, DateOnly date)
        {
            return await _context.ServiceProgresses
                .Include(sp => sp.Shift)
                .Include(sp => sp.Caretaker)
                .Include(sp => sp.ServiceRegistration)
                    .ThenInclude(r => r!.NurseryCareService)
                        .ThenInclude(ncs => ncs!.CareServicePackage)
                .Include(sp => sp.ServiceRegistration)
                    .ThenInclude(r => r!.User)
                .Where(sp => sp.TaskDate == date
                    && sp.ServiceRegistration != null
                    && sp.ServiceRegistration.NurseryCareService != null
                    && sp.ServiceRegistration.NurseryCareService.NurseryId == nurseryId)
                .OrderBy(sp => sp.Caretaker != null ? sp.Caretaker.Username : string.Empty)
                    .ThenBy(sp => sp.Shift.StartTime)
                .ToListAsync();
        }

        public async Task<List<ServiceProgress>> GetByCaretakerAndDateRangeAsync(int nurseryId, int caretakerId, DateOnly from, DateOnly to)
        {
            return await _context.ServiceProgresses
                .Include(sp => sp.Shift)
                .Include(sp => sp.Caretaker)
                .Include(sp => sp.ServiceRegistration)
                    .ThenInclude(r => r!.NurseryCareService)
                        .ThenInclude(ncs => ncs!.CareServicePackage)
                .Include(sp => sp.ServiceRegistration)
                    .ThenInclude(r => r!.User)
                .Where(sp => sp.CaretakerId == caretakerId
                    && sp.TaskDate >= from && sp.TaskDate <= to
                    && sp.ServiceRegistration != null
                    && sp.ServiceRegistration.NurseryCareService != null
                    && sp.ServiceRegistration.NurseryCareService.NurseryId == nurseryId)
                .OrderBy(sp => sp.TaskDate)
                    .ThenBy(sp => sp.Shift.StartTime)
                .ToListAsync();
        }

        public async Task<List<ServiceProgress>> GetByCaretakerSelfDateRangeAsync(int caretakerId, DateOnly from, DateOnly to)
        {
            return await _context.ServiceProgresses
                .Include(sp => sp.Shift)
                .Include(sp => sp.ServiceRegistration)
                    .ThenInclude(r => r!.NurseryCareService)
                        .ThenInclude(ncs => ncs!.CareServicePackage)
                .Include(sp => sp.ServiceRegistration)
                    .ThenInclude(r => r!.NurseryCareService)
                        .ThenInclude(ncs => ncs!.Nursery)
                .Include(sp => sp.ServiceRegistration)
                    .ThenInclude(r => r!.User)
                .Where(sp => sp.CaretakerId == caretakerId
                    && sp.TaskDate >= from && sp.TaskDate <= to)
                .OrderBy(sp => sp.TaskDate)
                    .ThenBy(sp => sp.Shift.StartTime)
                .ToListAsync();
        }

        public async Task<HashSet<int>> GetConflictingCaretakerIdsAsync(int shiftId, List<DateOnly> dates)
        {
            var ids = await _context.ServiceProgresses
                .Where(sp => sp.ShiftId == shiftId
                    && sp.TaskDate.HasValue && dates.Contains(sp.TaskDate.Value)
                    && (sp.Status == (int)ServiceProgressStatusEnum.Pending
                        || sp.Status == (int)ServiceProgressStatusEnum.InProgress
                        || sp.Status == (int)ServiceProgressStatusEnum.Assigned)
                    && sp.CaretakerId != null)
                .Select(sp => sp.CaretakerId!.Value)
                .Distinct()
                .ToListAsync();
            return ids.ToHashSet();
        }
    }
}
