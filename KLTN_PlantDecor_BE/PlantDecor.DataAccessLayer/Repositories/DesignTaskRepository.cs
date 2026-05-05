using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class DesignTaskRepository : GenericRepository<DesignTask>, IDesignTaskRepository
    {
        public DesignTaskRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<DesignTask?> GetByIdWithDetailsAsync(int id)
        {
            return await BuildDetailedQuery()
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<List<DesignTask>> GetByRegistrationIdAsync(int designRegistrationId)
        {
            return await BuildDetailedQuery()
                .Where(x => x.DesignRegistrationId == designRegistrationId)
                .OrderBy(x => x.ScheduledDate)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<List<DesignTask>> GetByAssignedStaffIdAndDateRangeAsync(int assignedStaffId, DateOnly from, DateOnly to)
        {
            return await BuildDetailedQuery()
                .Where(x => x.AssignedStaffId == assignedStaffId
                    && x.ScheduledDate.HasValue
                    && x.ScheduledDate.Value >= from
                    && x.ScheduledDate.Value <= to)
                .OrderBy(x => x.ScheduledDate)
                .ThenBy(x => x.Id)
                .ToListAsync();
        }

        public async Task<PaginatedResult<DesignTask>> GetByAssignedStaffIdAsync(
            int assignedStaffId,
            Pagination pagination,
            int? status = null,
            DateOnly? from = null,
            DateOnly? to = null)
        {
            var query = BuildDetailedQuery()
                .Where(x => x.AssignedStaffId == assignedStaffId);

            if (status.HasValue)
            {
                query = query.Where(x => x.Status == status.Value);
            }

            if (from.HasValue)
            {
                query = query.Where(x => x.ScheduledDate.HasValue && x.ScheduledDate.Value >= from.Value);
            }

            if (to.HasValue)
            {
                query = query.Where(x => x.ScheduledDate.HasValue && x.ScheduledDate.Value <= to.Value);
            }

            query = query.OrderByDescending(x => x.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<DesignTask>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        private IQueryable<DesignTask> BuildDetailedQuery()
        {
            return _context.DesignTasks
                .AsNoTracking()
                .Include(x => x.AssignedStaff)
                .Include(x => x.DesignRegistration)
                    .ThenInclude(r => r.User)
                .Include(x => x.DesignRegistration)
                    .ThenInclude(r => r.Nursery)
                .Include(x => x.DesignRegistration)
                    .ThenInclude(r => r.DesignTemplateTier)
                        .ThenInclude(t => t.DesignTemplate)
                .Include(x => x.TaskMaterialUsages)
                    .ThenInclude(u => u.Material);
        }
    }
}