using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class PlantComboRepository : GenericRepository<PlantCombo>, IPlantComboRepository
    {
        public PlantComboRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<PaginatedResult<PlantCombo>> GetAllWithDetailsAsync(Pagination pagination)
        {
            var query = _context.PlantCombos
                .Include(c => c.PlantComboItems)
                    .ThenInclude(ci => ci.Plant)
                .Include(c => c.PlantComboImages)
                .Include(c => c.TagsNavigation)
                .OrderByDescending(c => c.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<PlantCombo>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<PlantCombo>> GetActiveWithDetailsAsync(Pagination pagination)
        {
            var query = _context.PlantCombos
                .Where(c => c.IsActive == true)
                .Include(c => c.PlantComboItems)
                    .ThenInclude(ci => ci.Plant)
                .Include(c => c.PlantComboImages)
                .Include(c => c.TagsNavigation)
                .OrderByDescending(c => c.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<PlantCombo>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PlantCombo?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.PlantCombos
                .Include(c => c.PlantComboItems)
                    .ThenInclude(ci => ci.Plant)
                .Include(c => c.PlantComboImages)
                .Include(c => c.TagsNavigation)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<PlantCombo?> GetByIdWithOrdersAsync(int id)
        {
            return await _context.PlantCombos
                .Include(c => c.NurseryPlantCombos)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<bool> ExistsByCodeAsync(string comboCode, int? excludeId = null)
        {
            if (excludeId.HasValue)
            {
                return await _context.PlantCombos
                    .AnyAsync(c => c.ComboCode == comboCode && c.Id != excludeId.Value);
            }
            return await _context.PlantCombos
                .AnyAsync(c => c.ComboCode == comboCode);
        }

        public async Task<PlantCombo?> GetComboByComboItemIdAsync(int comboItemId)
        {
            return await _context.PlantCombos
                .Include(c => c.PlantComboItems)
                    .ThenInclude(ci => ci.Plant)
                .Include(c => c.PlantComboImages)
                .Include(c => c.TagsNavigation)
                .FirstOrDefaultAsync(c => c.PlantComboItems.Any(ci => ci.Id == comboItemId));
        }

        public async Task<PaginatedResult<PlantCombo>> GetCombosForShopAsync(Pagination pagination)
        {
            var query = _context.PlantCombos
                .Where(c => c.IsActive == true)
                .Include(c => c.PlantComboItems)
                    .ThenInclude(ci => ci.Plant)
                .Include(c => c.PlantComboImages)
                .Include(c => c.TagsNavigation)
                .OrderByDescending(c => c.CreatedAt);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<PlantCombo>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }
    }
}
