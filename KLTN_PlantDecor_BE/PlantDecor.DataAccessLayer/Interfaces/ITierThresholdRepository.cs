using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ITierThresholdRepository : IGenericRepository<TierThreshold>
    {
        Task<IEnumerable<TierThreshold>> GetAllActiveAsync();
        Task<TierThreshold?> GetByTierLevelAsync(int tierLevel);
    }
}
