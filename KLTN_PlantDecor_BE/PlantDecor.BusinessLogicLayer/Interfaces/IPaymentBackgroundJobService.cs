namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IPaymentBackgroundJobService
    {
        /// <summary>
        /// Auto-expire pending transactions that have passed their ExpiredAt time
        /// </summary>
        Task ExpireTransactionsAsync();
    }
}
