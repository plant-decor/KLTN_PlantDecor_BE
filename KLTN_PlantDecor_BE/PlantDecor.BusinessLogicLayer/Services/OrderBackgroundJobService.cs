using Microsoft.Extensions.Logging;
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
                    _unitOfWork.NurseryOrderRepository.PrepareUpdate(nurseryOrder);
                }

                // Collect all InvoiceDetails from all NurseryOrders
                var invoiceDetails = order.NurseryOrders
                    .SelectMany(no => no.NurseryOrderDetails)
                    .Select(d => new InvoiceDetail
                    {
                        ItemName = d.ItemName,
                        UnitPrice = d.UnitPrice,
                        Quantity = d.Quantity,
                        Amount = d.Amount
                    })
                    .ToList();

                // Create single RemainingBalance Invoice for the entire Order
                var invoice = new Invoice
                {
                    OrderId = orderId,
                    Type = (int)InvoiceTypeEnum.RemainingBalance,
                    TotalAmount = order.RemainingAmount ?? 0,
                    Status = (int)InvoiceStatusEnum.Pending,
                    IssuedDate = DateTime.Now,
                    InvoiceDetails = invoiceDetails
                };

                _unitOfWork.InvoiceRepository.PrepareCreate(invoice);

                // Save all changes once
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
