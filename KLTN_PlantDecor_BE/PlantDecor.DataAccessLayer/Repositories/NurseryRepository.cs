using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class NurseryRepository : GenericRepository<Nursery>, INurseryRepository
    {
        public NurseryRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<PaginatedResult<Nursery>> GetAllWithDetailsAsync(Pagination pagination)
        {
            var query = _context.Nurseries
                .Include(n => n.Manager)
                .OrderByDescending(n => n.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Nursery>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<Nursery?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.Nurseries
                .Include(n => n.Manager)
                .Include(n => n.CommonPlants)
                    .ThenInclude(cp => cp.Plant)
                .Include(n => n.NurseryMaterials)
                    .ThenInclude(nm => nm.Material)
                .FirstOrDefaultAsync(n => n.Id == id);
        }

        public async Task<Nursery?> GetByManagerIdAsync(int managerId)
        {
            return await _context.Nurseries
                .Include(n => n.Manager)
                .FirstOrDefaultAsync(n => n.ManagerId == managerId);
        }

        public async Task<bool> ExistsByNameAsync(string name, int? excludeId = null)
        {
            var query = _context.Nurseries.Where(n => n.Name == name);
            if (excludeId.HasValue)
                query = query.Where(n => n.Id != excludeId.Value);
            return await query.AnyAsync();
        }

        public async Task<PaginatedResult<Nursery>> GetActiveNurseriesAsync(Pagination pagination)
        {
            var query = _context.Nurseries
                .Where(n => n.IsActive == true)
                .Include(n => n.Manager)
                .OrderByDescending(n => n.Id);

            var totalCount = await query.CountAsync();
            var items = await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<Nursery>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<List<Nursery>> GetNearbyWithPackageAsync(decimal lat, decimal lng, decimal radiusKm, int? packageId)
        {
            var query = _context.Nurseries
                .Include(n => n.Manager)
                .Include(n => n.NurseryCareServices)
                    .ThenInclude(ncs => ncs.CareServicePackage)
                .Where(n => n.IsActive == true && n.Latitude.HasValue && n.Longitude.HasValue);

            if (packageId.HasValue)
                query = query.Where(n => n.NurseryCareServices.Any(ncs => ncs.IsActive && ncs.CareServicePackageId == packageId.Value));

            var nurseries = await query.ToListAsync();

            return nurseries
                .Where(n => HaversineKm((double)lat, (double)lng, (double)n.Latitude!.Value, (double)n.Longitude!.Value) <= (double)radiusKm)
                .OrderBy(n => HaversineKm((double)lat, (double)lng, (double)n.Latitude!.Value, (double)n.Longitude!.Value))
                .ToList();
        }

        private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371.0;
            var dLat = (lat2 - lat1) * Math.PI / 180;
            var dLon = (lon2 - lon1) * Math.PI / 180;
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                  + Math.Cos(lat1 * Math.PI / 180) * Math.Cos(lat2 * Math.PI / 180)
                  * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        }
    }
}
