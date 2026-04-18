using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
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