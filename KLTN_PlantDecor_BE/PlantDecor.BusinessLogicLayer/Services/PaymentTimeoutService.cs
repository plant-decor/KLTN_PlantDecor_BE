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
            var now = DateTime.Now;
            var pendingPayments = await _unitOfWork.PaymentRepository.GetPendingWithTransactionsAsync();

            var updatedPaymentCount = 0;
            var updatedTransactionCount = 0;

            foreach (var payment in pendingPayments)
            {
                var expiredPendingTransactions = payment.Transactions
                    .Where(t => t.Status == (int)TransactionStatusEnum.Pending
                        && t.ExpiredAt.HasValue
                        && t.ExpiredAt.Value <= now)
                    .ToList();

                if (!expiredPendingTransactions.Any())
                    continue;

                foreach (var transaction in expiredPendingTransactions)
                {
                    transaction.Status = (int)TransactionStatusEnum.TimedOut;
                    _unitOfWork.TransactionRepository.PrepareUpdate(transaction);
                    updatedTransactionCount++;
                }

                payment.Status = (int)PaymentStatusEnum.Failed;
                _unitOfWork.PaymentRepository.PrepareUpdate(payment);
                updatedPaymentCount++;
            }

            if (updatedPaymentCount > 0 || updatedTransactionCount > 0)
            {
                await _unitOfWork.SaveAsync();
            }

            _logger.LogInformation(
                "Payment timeout job completed. Updated payments: {PaymentCount}, updated transactions: {TransactionCount}",
                updatedPaymentCount,
                updatedTransactionCount);
        }
    }
}
