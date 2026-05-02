namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IOrderBackgroundJobService
    {
        /// <summary>
        /// Process order after delivery - check payment strategy and create RemainingBalance invoice if needed
        /// </summary>
        /// <param name="orderId">Order ID to process</param>
        Task ProcessOrderDeliveryAsync(int orderId);

        /// <summary>
        /// Add purchased plants to My Plant for a delivered order
        /// </summary>
        /// <param name="orderId">Order ID to process</param>
        /// <param name="purchasedAt">Date and time when delivery was confirmed</param>
        Task AddPurchasedPlantsToMyPlantAsync(int orderId, DateTime purchasedAt);

        /// <summary>
        /// Auto-complete orders that stay in PendingConfirmation for 3 days
        /// </summary>
        Task AutoCompletePendingConfirmationOrdersAsync();
    }
}
