using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class LayoutDesignRepository : GenericRepository<LayoutDesign>, ILayoutDesignRepository
    {
        public LayoutDesignRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<PaginatedResult<LayoutDesign>> GetAllByUserIdWithDetailsAsync(int userId, Pagination pagination)
        {
            var query = _context.LayoutDesigns
                .AsNoTracking()
                .Where(layout => layout.UserId == userId)
                .Include(layout => layout.LayoutDesignRoomImages)
                    .ThenInclude(layoutRoomImage => layoutRoomImage.RoomImage)
                .Include(layout => layout.LayoutDesignPlants)
                .Include(layout => layout.LayoutDesignAiResponseImages)
                .OrderByDescending(layout => layout.CreatedAt)
                .ThenByDescending(layout => layout.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<LayoutDesign>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<LayoutDesign?> GetGenerationContextByIdAsync(int layoutDesignId)
        {
            return await _context.LayoutDesigns
                .AsNoTracking()
                .Include(layout => layout.LayoutDesignRoomImages)
                    .ThenInclude(layoutRoomImage => layoutRoomImage.RoomImage)
                .Include(layout => layout.LayoutDesignPlants)
                .FirstOrDefaultAsync(layout => layout.Id == layoutDesignId);
        }

        public async Task<int?> GetOwnerIdAsync(int layoutDesignId)
        {
            return await _context.LayoutDesigns
                .AsNoTracking()
                .Where(layout => layout.Id == layoutDesignId)
                .Select(layout => layout.UserId)
                .FirstOrDefaultAsync();
        }
    }
}
