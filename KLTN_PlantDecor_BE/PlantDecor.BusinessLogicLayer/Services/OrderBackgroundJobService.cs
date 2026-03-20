using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class OrderBackgroundJobService : IOrderBackgroundJobService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<OrderBackgroundJobService> _logger;

        public OrderBackgroundJobService(IUnitOfWork unitOfWork, ILogger<OrderBackgroundJobService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task ProcessOrderDeliveryAsync(int orderId)
        {
            try
            {
                _logger.LogInformation("Processing order delivery for Order {OrderId}", orderId);

                var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found", orderId);
                    return;
                }

                // Check if PaymentStrategy is Deposit
                if (order.PaymentStrategy != (int)PaymentStrategiesEnum.Deposit)
                {
                    _logger.LogInformation("Order {OrderId} does not use Deposit strategy, skipping", orderId);
                    return;
                }

                // Check if order is in Delivered status
                if (order.Status != (int)OrderStatusEnum.Delivered)
                {
                    _logger.LogWarning("Order {OrderId} is not in Delivered status, current status: {Status}", orderId, order.Status);
                    return;
                }

                // Check if RemainingBalance invoice already exists
                var existingInvoice = await _unitOfWork.InvoiceRepository
                    .GetPendingByOrderIdAndTypeAsync(orderId, (int)InvoiceTypeEnum.RemainingBalance);

                if (existingInvoice != null)
                {
                    _logger.LogInformation("RemainingBalance invoice already exists for Order {OrderId}", orderId);
                    return;
                }

                // Update Order status to RemainingPaymentPending
                order.Status = (int)OrderStatusEnum.RemainingPaymentPending;
                order.UpdatedAt = DateTime.Now;
                _unitOfWork.OrderRepository.PrepareUpdate(order);

                // Update all NurseryOrder status to match parent Order status
                foreach (var nurseryOrder in order.NurseryOrders)
                {
                    nurseryOrder.Status = (int)OrderStatusEnum.RemainingPaymentPending;
                    nurseryOrder.UpdatedAt = DateTime.Now;
                }

                // Create RemainingBalance Invoice
                var invoice = new Invoice
                {
                    OrderId = orderId,
                    NurseryId = order.NurseryId,
                    Type = (int)InvoiceTypeEnum.RemainingBalance,
                    TotalAmount = order.RemainingAmount ?? 0,
                    Status = (int)InvoiceStatusEnum.Pending,
                    IssuedDate = DateTime.Now
                };

                _unitOfWork.InvoiceRepository.PrepareCreate(invoice);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Successfully created RemainingBalance invoice for Order {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing order delivery for Order {OrderId}", orderId);
                throw;
            }
        }
    }
}
