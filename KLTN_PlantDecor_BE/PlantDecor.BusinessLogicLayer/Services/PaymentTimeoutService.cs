using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class PaymentTimeoutService : IPaymentTimeoutService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<PaymentTimeoutService> _logger;

        public PaymentTimeoutService(IUnitOfWork unitOfWork, ILogger<PaymentTimeoutService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ProcessExpiredPaymentsAsync()
        {
            try
            {
                _logger.LogInformation("Starting auto-expire pending transactions job");

                var expiredTransactions = await _unitOfWork.TransactionRepository.GetExpiredPendingTransactionsAsync();

                if (!expiredTransactions.Any())
                {
                    _logger.LogInformation("No expired pending transactions found");
                    return;
                }

                _logger.LogInformation("Found {Count} expired pending transactions to update", expiredTransactions.Count);

                foreach (var transaction in expiredTransactions)
                {
                    transaction.Status = (int)TransactionStatusEnum.TimedOut;
                    _unitOfWork.TransactionRepository.PrepareUpdate(transaction);
                    _logger.LogInformation("Expired transaction {TransactionId}", transaction.TransactionId);
                }

                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Successfully expired {Count} transactions. Payments remain Pending to allow retry.", expiredTransactions.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error expiring transactions");
                throw;
            }
        }
    }
}
