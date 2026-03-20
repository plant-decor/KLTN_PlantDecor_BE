namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IPaymentTimeoutService
    {
        Task ProcessExpiredPaymentsAsync();
    }
}
