namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ISubscriptionExpiryService
    {
        Task DeactivateSubscriptionAsync(int subscriptionId);
    }
}
