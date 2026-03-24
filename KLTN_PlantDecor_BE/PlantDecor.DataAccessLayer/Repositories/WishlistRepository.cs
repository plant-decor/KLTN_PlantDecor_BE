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
                .Include(w => w.CommonPlant)
                    .ThenInclude(cp => cp.Plant)
                    .ThenInclude(p => p.PlantImages)
                .Include(w => w.PlantInstance)
                    .ThenInclude(pi => pi.PlantImages)
                .Include(w => w.PlantInstance)
                    .ThenInclude(pi => pi.Plant)
                .Include(w => w.NurseryPlantCombo)
                    .ThenInclude(npc => npc.PlantCombo)
                    .ThenInclude(pc => pc.PlantComboImages)
                .Include(w => w.NurseryMaterial)
                    .ThenInclude(nm => nm.Material)
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
                .Include(w => w.CommonPlant)
                    .ThenInclude(cp => cp.Plant)
                    .ThenInclude(p => p.PlantImages)
                .Include(w => w.PlantInstance)
                    .ThenInclude(pi => pi.PlantImages)
                .Include(w => w.PlantInstance)
                    .ThenInclude(pi => pi.Plant)
                .Include(w => w.NurseryPlantCombo)
                    .ThenInclude(npc => npc.PlantCombo)
                    .ThenInclude(pc => pc.PlantComboImages)
                .Include(w => w.NurseryMaterial)
                    .ThenInclude(nm => nm.Material)
                    .ThenInclude(m => m.MaterialImages)
                .Where(w => w.UserId == userId && w.ItemType == itemType && !w.IsDeleted);

            return itemType switch
            {
                WishlistItemType.CommonPlant => await query.FirstOrDefaultAsync(w => w.CommonPlantId == itemId),
                WishlistItemType.PlantInstance => await query.FirstOrDefaultAsync(w => w.PlantInstanceId == itemId),
                WishlistItemType.NurseryPlantCombo => await query.FirstOrDefaultAsync(w => w.NurseryPlantComboId == itemId),
                WishlistItemType.NurseryMaterial => await query.FirstOrDefaultAsync(w => w.NurseryMaterialId == itemId),
                _ => null
            };
        }

        public async Task<bool> ExistsAsync(int userId, WishlistItemType itemType, int itemId)
        {
            var query = _context.Wishlists
                .Where(w => w.UserId == userId && w.ItemType == itemType && !w.IsDeleted);

            return itemType switch
            {
                WishlistItemType.CommonPlant => await query.AnyAsync(w => w.CommonPlantId == itemId),
                WishlistItemType.PlantInstance => await query.AnyAsync(w => w.PlantInstanceId == itemId),
                WishlistItemType.NurseryPlantCombo => await query.AnyAsync(w => w.NurseryPlantComboId == itemId),
                WishlistItemType.NurseryMaterial => await query.AnyAsync(w => w.NurseryMaterialId == itemId),
                _ => false
            };
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
