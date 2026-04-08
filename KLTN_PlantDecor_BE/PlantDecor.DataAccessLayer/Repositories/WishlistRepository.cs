using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
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
                .Include(w => w.PlantInstance)
                    .ThenInclude(pi => pi.PlantImages)
                .Include(w => w.PlantInstance)
                    .ThenInclude(pi => pi.Plant)
                .Include(w => w.PlantInstance)
                    .ThenInclude(pi => pi.CurrentNursery)
                .Include(w => w.PlantCombo)
                    .ThenInclude(pc => pc.PlantComboImages)
                .Include(w => w.Material)
                    .ThenInclude(m => m.MaterialImages)
                .Where(w => w.UserId == userId && !w.IsDeleted)
                .OrderByDescending(w => w.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip(pagination.Skip).Take(pagination.Take).ToListAsync();

            return new PaginatedResult<Wishlist>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<Wishlist?> GetByUserAndItemAsync(int userId, WishlistItemType itemType, int itemId)
        {
            var query = _context.Wishlists
                .Include(w => w.Plant)
                    .ThenInclude(p => p.PlantImages)
                .Include(w => w.PlantInstance)
                    .ThenInclude(pi => pi.PlantImages)
                .Include(w => w.PlantInstance)
                    .ThenInclude(pi => pi.Plant)
                .Include(w => w.PlantInstance)
                    .ThenInclude(pi => pi.CurrentNursery)
                .Include(w => w.PlantCombo)
                    .ThenInclude(pc => pc.PlantComboImages)
                .Include(w => w.Material)
                    .ThenInclude(m => m.MaterialImages)
                .Where(w => w.UserId == userId && w.ItemType == itemType && !w.IsDeleted);

            return itemType switch
            {
                WishlistItemType.Plant => await query.FirstOrDefaultAsync(w => w.PlantId == itemId),
                WishlistItemType.PlantInstance => await query.FirstOrDefaultAsync(w => w.PlantInstanceId == itemId),
                WishlistItemType.PlantCombo => await query.FirstOrDefaultAsync(w => w.PlantComboId == itemId),
                WishlistItemType.Material => await query.FirstOrDefaultAsync(w => w.MaterialId == itemId),
                _ => null
            };
        }

        public async Task<bool> ExistsAsync(int userId, WishlistItemType itemType, int itemId)
        {
            var query = _context.Wishlists
                .Where(w => w.UserId == userId && w.ItemType == itemType && !w.IsDeleted);

            return itemType switch
            {
                WishlistItemType.Plant => await query.AnyAsync(w => w.PlantId == itemId),
                WishlistItemType.PlantInstance => await query.AnyAsync(w => w.PlantInstanceId == itemId),
                WishlistItemType.PlantCombo => await query.AnyAsync(w => w.PlantComboId == itemId),
                WishlistItemType.Material => await query.AnyAsync(w => w.MaterialId == itemId),
                _ => false
            };
        }

        public async Task<int> ClearByUserIdAsync(int userId)
        {
            var wishlists = await _context.Wishlists
                .Where(w => w.UserId == userId && !w.IsDeleted)
                .ToListAsync();

            if (wishlists.Count == 0)
                return 0;

            var now = DateTime.UtcNow;
            foreach (var wishlist in wishlists)
            {
                wishlist.IsDeleted = true;
                wishlist.DeletedAt = now;
            }

            await _context.SaveChangesAsync();
            return wishlists.Count;
        }

        public async Task SoftDeletePlantInstanceWishlistsAsync(int plantInstanceId)
        {
            var wishlists = await _context.Wishlists
                .Where(w => w.PlantInstanceId == plantInstanceId && !w.IsDeleted)
                .ToListAsync();

            foreach (var wishlist in wishlists)
            {
                wishlist.IsDeleted = true;
                wishlist.DeletedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }
    }
}
