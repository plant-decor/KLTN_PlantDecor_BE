using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IDepositPolicyRepository : IGenericRepository<DepositPolicy>
    {
        Task<List<DepositPolicy>> GetAllOrderedAsync();
        Task<DepositPolicy?> GetMatchingActivePolicyByPriceAsync(decimal price);
        Task<bool> HasOverlappingActiveRangeAsync(decimal minPrice, decimal? maxPrice, int? excludeId = null);
    }
}
