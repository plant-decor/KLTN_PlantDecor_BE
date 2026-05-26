using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ITierPackageRepository : IGenericRepository<TierPackage>
    {
        Task<IEnumerable<TierPackage>> GetAllActiveAsync();
        Task<TierPackage?> GetDefaultFreePackageAsync();
    }
}
