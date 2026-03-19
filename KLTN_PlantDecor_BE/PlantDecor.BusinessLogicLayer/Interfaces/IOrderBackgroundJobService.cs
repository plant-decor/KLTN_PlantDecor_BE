namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IOrderBackgroundJobService
    {
        /// <summary>
        /// Process order after delivery - check payment strategy and create RemainingBalance invoice if needed
        /// </summary>
        /// <param name="orderId">Order ID to process</param>
        Task ProcessOrderDeliveryAsync(int orderId);
    }
}
