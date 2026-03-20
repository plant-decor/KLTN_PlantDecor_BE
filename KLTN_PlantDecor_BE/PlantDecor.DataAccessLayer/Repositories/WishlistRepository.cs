using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class WishlistRepository : GenericRepository<Wishlist>, IWishlistRepository
    {
        public WishlistRepository(PlantDecorContext context) : base(context) { }

        public async Task<PaginatedResult<Wishlist>> GetByUserIdWithPaginationAsync(int userId, Pagination pagination)
        {
            var query = _context.Wishlists
                .Include(w => w.Plant)
                    .ThenInclude(p => p.PlantImages)
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip(pagination.Skip).Take(pagination.Take).ToListAsync();

            return new PaginatedResult<Wishlist>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<Wishlist?> GetByUserAndPlantAsync(int userId, int plantId)
        {
            return await _context.Wishlists
                .Include(w => w.Plant)
                    .ThenInclude(p => p.PlantImages)
                .FirstOrDefaultAsync(w => w.UserId == userId && w.PlantId == plantId);
        }

        public async Task<bool> ExistsAsync(int userId, int plantId)
        {
            return await _context.Wishlists
                .AnyAsync(w => w.UserId == userId && w.PlantId == plantId);
        }
    }
}
