using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class DesignRegistrationRepository : GenericRepository<DesignRegistration>, IDesignRegistrationRepository
    {
        public DesignRegistrationRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<DesignRegistration?> GetByIdWithDetailsAsync(int id)
        {
            return await BuildDetailedQuery()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<PaginatedResult<DesignRegistration>> GetByUserIdAsync(int userId, Pagination pagination, int? status = null)
        {
            var query = BuildDetailedQuery()
                .Where(x => x.UserId == userId);

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            query = query.OrderByDescending(x => x.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<DesignRegistration>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<DesignRegistration>> GetPendingByNurseryIdAsync(int nurseryId, Pagination pagination)
        {
            var pendingStatuses = new[]
            {
                (int)DesignRegistrationStatus.PendingApproval
            };

            var query = BuildDetailedQuery()
                .Where(x => x.NurseryId == nurseryId && pendingStatuses.Contains(x.Status))
                .OrderByDescending(x => x.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<DesignRegistration>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<DesignRegistration>> GetByNurseryIdAsync(int nurseryId, Pagination pagination, int? status = null)
        {
            var query = BuildDetailedQuery()
                .Where(x => x.NurseryId == nurseryId);

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            query = query.OrderByDescending(x => x.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<DesignRegistration>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<DesignRegistration?> GetByOrderIdAsync(int orderId)
        {
            return await BuildDetailedQuery()
                .FirstOrDefaultAsync(x => x.OrderId == orderId);
        }

        public async Task<PaginatedResult<DesignRegistration>> GetByAssignedCaretakerIdAsync(int caretakerId, Pagination pagination, int? status = null)
        {
            var query = BuildDetailedQuery()
                .Where(x => x.AssignedCaretakerId.HasValue && x.AssignedCaretakerId.Value == caretakerId);

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            query = query.OrderByDescending(x => x.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<DesignRegistration>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<DesignRegistration>> GetByAssignedCaretakerIdWithStatusesAsync(int caretakerId, List<int> statuses, Pagination pagination)
        {
            if (statuses == null || statuses.Count == 0)
            {
                return await GetByAssignedCaretakerIdAsync(caretakerId, pagination, status: null);
            }

            var query = BuildDetailedQuery()
                .Where(x => x.AssignedCaretakerId.HasValue && x.AssignedCaretakerId.Value == caretakerId && statuses.Contains(x.Status));

            query = query.OrderByDescending(x => x.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<DesignRegistration>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<Dictionary<int, int>> CountOpenByNurseryIdsAsync(List<int> nurseryIds)
        {
            if (nurseryIds == null || nurseryIds.Count == 0)
            {
                return new Dictionary<int, int>();
            }

            var openStatuses = new[]
            {
                (int)DesignRegistrationStatus.PendingApproval,
                (int)DesignRegistrationStatus.AwaitDeposit,
                (int)DesignRegistrationStatus.DepositPaid,
                (int)DesignRegistrationStatus.InProgress,
                (int)DesignRegistrationStatus.AwaitFinalPayment
            };

            return await _context.DesignRegistrations
                .Where(x => nurseryIds.Contains(x.NurseryId)
                            && openStatuses.Contains(x.Status))
                .GroupBy(x => x.NurseryId)
                .Select(g => new { NurseryId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.NurseryId, x => x.Count);
        }

        public async Task<Dictionary<int, int>> CountOpenAssignmentsByCaretakerIdsAsync(List<int> caretakerIds, List<int> nurseryIds)
        {
            if (caretakerIds == null || caretakerIds.Count == 0)
            {
                return new Dictionary<int, int>();
            }

            if (nurseryIds == null || nurseryIds.Count == 0)
            {
                return new Dictionary<int, int>();
            }

            var openStatuses = new[]
            {
                (int)DesignRegistrationStatus.DepositPaid,
                (int)DesignRegistrationStatus.InProgress,
                (int)DesignRegistrationStatus.AwaitFinalPayment
            };

            return await _context.DesignRegistrations
                .AsNoTracking()
                .Where(x => nurseryIds.Contains(x.NurseryId)
                            && x.AssignedCaretakerId.HasValue
                            && caretakerIds.Contains(x.AssignedCaretakerId.Value)
                            && openStatuses.Contains(x.Status))
                .GroupBy(x => x.AssignedCaretakerId!.Value)
                .Select(g => new { CaretakerId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.CaretakerId, x => x.Count);
        }

        private IQueryable<DesignRegistration> BuildDetailedQuery()
        {
            return _context.DesignRegistrations
                .AsNoTracking()
                .Include(x => x.User)
                .Include(x => x.AssignedCaretaker)
                .Include(x => x.Nursery)
                .Include(x => x.Order)
                .Include(x => x.DesignTemplateTier)
                    .ThenInclude(t => t.DesignTemplate)
                .Include(x => x.DesignTasks)
                    .ThenInclude(t => t.AssignedStaff)
                .Include(x => x.DesignTasks)
                    .ThenInclude(t => t.TaskMaterialUsages)
                        .ThenInclude(u => u.Material);
        }
    }
}
