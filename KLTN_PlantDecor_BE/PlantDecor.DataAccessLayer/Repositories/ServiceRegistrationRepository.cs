using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class ServiceRegistrationRepository : GenericRepository<ServiceRegistration>, IServiceRegistrationRepository
    {
        public ServiceRegistrationRepository(PlantDecorContext context) : base(context) { }

        public async Task<ServiceRegistration?> GetByIdWithDetailsAsync(int id)
        {
            return await BuildDetailedQuery()
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<PaginatedResult<ServiceRegistration>> GetByUserIdAsync(int userId, Pagination pagination, int? status = null)
        {
            var query = BuildDetailedQuery()
                .Where(r => r.UserId == userId);

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            query = query.OrderByDescending(r => r.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<ServiceRegistration>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<ServiceRegistration>> GetPendingByNurseryIdAsync(int nurseryId, Pagination pagination)
        {
            var pendingStatuses = new[]
            {
                (int)ServiceRegistrationStatusEnum.WaitingForNursery,
                (int)ServiceRegistrationStatusEnum.PendingApproval
            };

            var query = BuildDetailedQuery()
                .Where(r => r.NurseryCareService != null &&
                            r.NurseryCareService.NurseryId == nurseryId &&
                            r.Status.HasValue &&
                            pendingStatuses.Contains(r.Status.Value))
                .OrderByDescending(r => r.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<ServiceRegistration>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<ServiceRegistration?> GetByOrderIdAsync(int orderId)
        {
            return await BuildDetailedQuery()
                .FirstOrDefaultAsync(r => r.OrderId == orderId);
        }

        public async Task<PaginatedResult<ServiceRegistration>> GetAllByNurseryIdAsync(int nurseryId, Pagination pagination, int? status = null)
        {
            var query = BuildDetailedQuery()
                .Where(r => r.NurseryCareService != null && r.NurseryCareService.NurseryId == nurseryId);

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            query = query.OrderByDescending(r => r.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<ServiceRegistration>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<ServiceRegistration>> GetByCaretakerIdAsync(int caretakerId, Pagination pagination, int? status = null)
        {
            var query = BuildDetailedQuery()
                .Where(r => r.MainCaretakerId == caretakerId || r.CurrentCaretakerId == caretakerId);

            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);

            query = query.OrderByDescending(r => r.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<ServiceRegistration>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<Dictionary<int, int>> CountOpenAssignmentsByCaretakerIdsAsync(List<int> caretakerIds, int nurseryId)
        {
            if (caretakerIds == null || caretakerIds.Count == 0)
            {
                return new Dictionary<int, int>();
            }

            var openStatuses = new[]
            {
                (int)ServiceRegistrationStatusEnum.Active
            };

            return await _context.ServiceRegistrations
                .Where(r => r.NurseryCareService != null
                            && r.NurseryCareService.NurseryId == nurseryId
                            && r.CurrentCaretakerId.HasValue
                            && caretakerIds.Contains(r.CurrentCaretakerId.Value)
                            && r.Status.HasValue
                            && openStatuses.Contains(r.Status.Value))
                .GroupBy(r => r.CurrentCaretakerId!.Value)
                .Select(g => new { CaretakerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CaretakerId, x => x.Count);
        }

        private IQueryable<ServiceRegistration> BuildDetailedQuery()
        {
            return _context.ServiceRegistrations
                .Include(r => r.User)
                .Include(r => r.NurseryCareService)
                    .ThenInclude(ncs => ncs!.CareServicePackage)
                .Include(r => r.NurseryCareService)
                    .ThenInclude(ncs => ncs!.Nursery)
                .Include(r => r.MainCaretaker)
                .Include(r => r.CurrentCaretaker)
                .Include(r => r.PrefferedShift)
                .Include(r => r.ServiceProgresses)
                    .ThenInclude(sp => sp.Shift)
                .Include(r => r.ServiceProgresses)
                    .ThenInclude(sp => sp.Caretaker)
                .Include(r => r.ServiceRating);
        }
    }
}
