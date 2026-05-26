using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IUserSubscriptionRepository : IGenericRepository<UserSubscription>
    {
        // Returns active subscriptions ordered by EndDate ASC (FIFO consumption)
        Task<IEnumerable<UserSubscription>> GetActiveByUserIdAsync(int userId);

        // Counts successful, non-refunded AI usages against a subscription
        Task<int> CountUsedQuotaAsync(int subscriptionId);

        // Deactivates all IsMonthlyFree subscriptions for a user (called before issuing a new monthly reset)
        Task DeactivateMonthlyFreeAsync(int userId);

        // Returns all subscriptions for a user ordered by CreatedAt DESC (history view)
        Task<IEnumerable<UserSubscription>> GetAllByUserIdAsync(int userId);
    }
}
