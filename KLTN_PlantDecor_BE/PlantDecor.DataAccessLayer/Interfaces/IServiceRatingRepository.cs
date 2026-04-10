using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IServiceRatingRepository : IGenericRepository<ServiceRating>
    {
        Task<ServiceRating?> GetByRegistrationIdAsync(int serviceRegistrationId);
        Task<bool> ExistsForRegistrationAsync(int serviceRegistrationId);
    }
}
