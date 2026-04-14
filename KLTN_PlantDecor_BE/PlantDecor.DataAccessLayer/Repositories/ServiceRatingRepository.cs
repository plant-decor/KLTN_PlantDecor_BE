using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class ServiceRatingRepository : GenericRepository<ServiceRating>, IServiceRatingRepository
    {
        public ServiceRatingRepository(PlantDecorContext context) : base(context) { }

        public async Task<ServiceRating?> GetByRegistrationIdAsync(int serviceRegistrationId)
        {
            return await _context.ServiceRatings
                .Include(r => r.User)
                .Include(r => r.ServiceRegistration)
                    .ThenInclude(sr => sr.NurseryCareService)
                        .ThenInclude(ncs => ncs.CareServicePackage)
                .FirstOrDefaultAsync(r => r.ServiceRegistrationId == serviceRegistrationId);
        }

        public async Task<bool> ExistsForRegistrationAsync(int serviceRegistrationId)
        {
            return await _context.ServiceRatings
                .AnyAsync(r => r.ServiceRegistrationId == serviceRegistrationId);
        }
    }
}
