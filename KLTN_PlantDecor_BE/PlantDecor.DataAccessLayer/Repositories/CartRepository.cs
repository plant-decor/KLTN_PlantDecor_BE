using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class CartRepository : GenericRepository<Cart>, ICartRepository
    {
        public CartRepository(PlantDecorContext context) : base(context) { }

        public async Task<Cart?> GetByUserIdAsync(int userId)
        {
            return await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.CommonPlant)
                        .ThenInclude(cp => cp!.Plant)
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.CommonPlant)
                        .ThenInclude(cp => cp!.Nursery)
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.NurseryPlantCombo)
                        .ThenInclude(npc => npc!.PlantCombo)
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.NurseryPlantCombo)
                        .ThenInclude(npc => npc!.Nursery)
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.NurseryMaterial)
                        .ThenInclude(nm => nm!.Material)
                .Include(c => c.CartItems)
                    .ThenInclude(i => i.NurseryMaterial)
                        .ThenInclude(nm => nm!.Nursery)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task<PaginatedResult<CartItem>> GetItemsByCartIdWithPaginationAsync(int cartId, Pagination pagination)
        {
            var query = _context.CartItems
                .Include(i => i.CommonPlant)
                    .ThenInclude(cp => cp!.Plant)
                .Include(i => i.CommonPlant)
                    .ThenInclude(cp => cp!.Nursery)
                .Include(i => i.NurseryPlantCombo)
                    .ThenInclude(npc => npc!.PlantCombo)
                .Include(i => i.NurseryPlantCombo)
                    .ThenInclude(npc => npc!.Nursery)
                .Include(i => i.NurseryMaterial)
                    .ThenInclude(nm => nm!.Material)
                .Include(i => i.NurseryMaterial)
                    .ThenInclude(nm => nm!.Nursery)
                .Where(i => i.CartId == cartId)
                .OrderByDescending(i => i.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query.Skip(pagination.Skip).Take(pagination.Take).ToListAsync();

            return new PaginatedResult<CartItem>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<CartItem?> GetCartItemByIdAsync(int cartItemId)
        {
            return await _context.CartItems
                .Include(i => i.Cart)
                .Include(i => i.CommonPlant)
                    .ThenInclude(cp => cp!.Plant)
                .Include(i => i.CommonPlant)
                    .ThenInclude(cp => cp!.Nursery)
                .Include(i => i.NurseryPlantCombo)
                    .ThenInclude(npc => npc!.PlantCombo)
                .Include(i => i.NurseryPlantCombo)
                    .ThenInclude(npc => npc!.Nursery)
                .Include(i => i.NurseryMaterial)
                    .ThenInclude(nm => nm!.Material)
                .Include(i => i.NurseryMaterial)
                    .ThenInclude(nm => nm!.Nursery)
                .FirstOrDefaultAsync(i => i.Id == cartItemId);
        }

        public async Task<bool> RemoveCartItemAsync(CartItem item)
        {
            _context.CartItems.Remove(item);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<int> ClearCartItemsAsync(int cartId)
        {
            var items = await _context.CartItems
                .Where(i => i.CartId == cartId)
                .ToListAsync();

            if (items.Count == 0) return 0;

            _context.CartItems.RemoveRange(items);
            return await _context.SaveChangesAsync();
        }
    }
}
