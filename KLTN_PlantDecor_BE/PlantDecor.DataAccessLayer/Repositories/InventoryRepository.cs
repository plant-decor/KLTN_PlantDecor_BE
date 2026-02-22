using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class InventoryRepository : GenericRepository<Inventory>, IInventoryRepository
    {
        public InventoryRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<List<Inventory>> GetAllWithDetailsAsync()
        {
            return await _context.Inventories
                .Include(i => i.Categories)
                .Include(i => i.Tags)
                .Include(i => i.InventoryImages)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Inventory>> GetActiveWithDetailsAsync()
        {
            return await _context.Inventories
                .Where(i => i.IsActive == true)
                .Include(i => i.Categories)
                .Include(i => i.Tags)
                .Include(i => i.InventoryImages)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
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

        public async Task<List<Inventory>> GetInventoriesForShopAsync()
        {
            return await _context.Inventories
                .Where(i => i.IsActive == true && i.StockQuantity > 0)
                .Include(i => i.Categories)
                .Include(i => i.Tags)
                .Include(i => i.InventoryImages)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }
    }
}
