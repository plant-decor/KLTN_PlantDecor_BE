using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class NurseryMaterialRepository : GenericRepository<NurseryMaterial>, INurseryMaterialRepository
    {
        public NurseryMaterialRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<PaginatedResult<NurseryMaterial>> GetAllWithDetailsAsync(Pagination pagination)
        {
            var query = _context.NurseryMaterials
                .Include(nm => nm.Material)
                .Include(nm => nm.Nursery)
                .OrderByDescending(nm => nm.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<NurseryMaterial>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<NurseryMaterial?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.NurseryMaterials
                .Include(nm => nm.Material)
                .Include(nm => nm.Nursery)
                .FirstOrDefaultAsync(nm => nm.Id == id);
        }

        public async Task<PaginatedResult<NurseryMaterial>> GetByNurseryIdAsync(int nurseryId, Pagination pagination)
        {
            var query = _context.NurseryMaterials
                .Where(nm => nm.NurseryId == nurseryId)
                .Include(nm => nm.Material)
                .Include(nm => nm.Nursery)
                .OrderByDescending(nm => nm.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<NurseryMaterial>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<PaginatedResult<NurseryMaterial>> GetByMaterialIdAsync(int materialId, Pagination pagination)
        {
            var query = _context.NurseryMaterials
                .Where(nm => nm.MaterialId == materialId)
                .Include(nm => nm.Material)
                .Include(nm => nm.Nursery)
                .OrderByDescending(nm => nm.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<NurseryMaterial>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<NurseryMaterial?> GetByMaterialAndNurseryAsync(int materialId, int nurseryId)
        {
            return await _context.NurseryMaterials
                .Include(nm => nm.Material)
                .Include(nm => nm.Nursery)
                .FirstOrDefaultAsync(nm => nm.MaterialId == materialId && nm.NurseryId == nurseryId);
        }

        public async Task<bool> ExistsAsync(int materialId, int nurseryId, int? excludeId = null)
        {
            var query = _context.NurseryMaterials
                .Where(nm => nm.MaterialId == materialId && nm.NurseryId == nurseryId);
            if (excludeId.HasValue)
                query = query.Where(nm => nm.Id != excludeId.Value);
            return await query.AnyAsync();
        }
    }
}
