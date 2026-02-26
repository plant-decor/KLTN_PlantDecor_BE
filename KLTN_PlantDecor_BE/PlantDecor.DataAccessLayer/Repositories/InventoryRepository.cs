using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class InventoryRepository : GenericRepository<Inventory>, IInventoryRepository
    {
        public InventoryRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<PaginatedResult<Inventory>> GetAllWithDetailsAsync(Pagination pagination)
        {
            var query = _context.Inventories
                .Include(i => i.Categories)
                .Include(i => i.Tags)
                .Include(i => i.InventoryImages)
                .OrderByDescending(i => i.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Inventory>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<Inventory>> GetActiveWithDetailsAsync(Pagination pagination)
        {
            var query = _context.Inventories
                .Where(i => i.IsActive == true)
                .Include(i => i.Categories)
                .Include(i => i.Tags)
                .Include(i => i.InventoryImages)
                .OrderByDescending(i => i.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Inventory>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<Inventory?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Inventories
                .Include(i => i.Categories)
                .Include(i => i.Tags)
                .Include(i => i.InventoryImages)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<Inventory?> GetByIdWithOrdersAsync(int id)
        {
            return await _context.Inventories
                .Include(i => i.CartItems)
                .Include(i => i.OrderItems)
                .FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task<bool> ExistsByCodeAsync(string inventoryCode, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await _context.Inventories
                    .AnyAsync(i => i.InventoryCode == inventoryCode && i.Id != excludeId.Value);
            }
            return await _context.Inventories
                .AnyAsync(i => i.InventoryCode == inventoryCode);
        }

        public async Task<PaginatedResult<Inventory>> GetInventoriesForShopAsync(Pagination pagination)
        {
            var query = _context.Inventories
                .Where(i => i.IsActive == true && i.StockQuantity > 0)
                .Include(i => i.Categories)
                .Include(i => i.Tags)
                .Include(i => i.InventoryImages)
                .OrderByDescending(i => i.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Inventory>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }
    }
}
