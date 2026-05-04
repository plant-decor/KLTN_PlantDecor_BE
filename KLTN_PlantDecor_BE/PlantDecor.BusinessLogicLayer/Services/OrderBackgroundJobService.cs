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
        private readonly IUserPlantService _userPlantService;

        public OrderBackgroundJobService(
            IUnitOfWork unitOfWork,
            ILogger<OrderBackgroundJobService> logger,
            IUserPlantService userPlantService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _userPlantService = userPlantService;
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

        public async Task AddPurchasedPlantsToMyPlantAsync(int orderId, DateTime purchasedAt)
        {
            try
            {
                _logger.LogInformation("Adding purchased plants to My Plant for Order {OrderId}", orderId);

                var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found", orderId);
                    return;
                }

                await _userPlantService.AddPurchasedPlantsToMyPlantAsync(order.Id, purchasedAt);

                _logger.LogInformation("Successfully added purchased plants to My Plant for Order {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while adding purchased plants to My Plant for Order {OrderId}", orderId);
                throw;
            }
        }

        public async Task AutoCompletePendingConfirmationOrdersAsync()
        {
            try
            {
                var threshold = DateTime.UtcNow.Date.AddDays(-3);
                _logger.LogInformation(
                    "Start auto-completing PendingConfirmation orders. Threshold: {Threshold}",
                    threshold);

                var orders = await _unitOfWork.OrderRepository.GetPendingConfirmationOrdersOlderThanAsync(threshold);

                if (!orders.Any())
                {
                    _logger.LogInformation("No PendingConfirmation orders older than 3 days found");
                    return;
                }

                var now = DateTime.UtcNow;

                foreach (var order in orders)
                {
                    order.Status = (int)OrderStatusEnum.Completed;
                    order.CompletedAt = now;
                    order.UpdatedAt = now;
                    _unitOfWork.OrderRepository.PrepareUpdate(order);

                    foreach (var nurseryOrder in order.NurseryOrders)
                    {
                        await UpdateInventoryForCompletedNurseryOrderAsync(nurseryOrder);
                        nurseryOrder.Status = (int)OrderStatusEnum.Completed;
                        nurseryOrder.UpdatedAt = now;
                        _unitOfWork.NurseryOrderRepository.PrepareUpdate(nurseryOrder);
                    }
                }

                await _unitOfWork.SaveAsync();

                _logger.LogInformation(
                    "Auto-completed {Count} orders from PendingConfirmation to Completed",
                    orders.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while auto-completing PendingConfirmation orders");
                throw;
            }
        }

        public async Task CompleteOrderIfAllNurseryOrdersCompletedAsync(int orderId, DateTime completedAt)
        {
            try
            {
                _logger.LogInformation("Checking nursery order completion for Order {OrderId}", orderId);

                var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Order {OrderId} not found", orderId);
                    return;
                }

                if (order.Status == (int)OrderStatusEnum.Completed)
                {
                    return;
                }

                if (order.Status != (int)OrderStatusEnum.PendingConfirmation)
                {
                    _logger.LogInformation(
                        "Order {OrderId} is not in PendingConfirmation status, current status: {Status}",
                        orderId,
                        order.Status);
                    return;
                }

                var allNurseryOrdersCompleted = order.NurseryOrders
                    .All(no => no.Status == (int)OrderStatusEnum.Completed);

                if (!allNurseryOrdersCompleted)
                {
                    _logger.LogInformation("Order {OrderId} has incomplete nursery orders", orderId);
                    return;
                }

                order.Status = (int)OrderStatusEnum.Completed;
                order.CompletedAt = completedAt;
                order.UpdatedAt = completedAt;
                _unitOfWork.OrderRepository.PrepareUpdate(order);

                await _unitOfWork.SaveAsync();

                _logger.LogInformation("Order {OrderId} marked as Completed", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while checking nursery order completion for Order {OrderId}", orderId);
                throw;
            }
        }

        private async Task UpdateInventoryForCompletedNurseryOrderAsync(NurseryOrder nurseryOrder)
        {
            foreach (var detail in nurseryOrder.NurseryOrderDetails)
            {
                var quantity = detail.Quantity ?? 0;
                if (quantity <= 0)
                {
                    continue;
                }

                if (detail.CommonPlantId.HasValue)
                {
                    var commonPlant = await _unitOfWork.CommonPlantRepository.GetByIdAsync(detail.CommonPlantId.Value);
                    if (commonPlant != null)
                    {
                        commonPlant.ReservedQuantity = Math.Max(0, commonPlant.ReservedQuantity - quantity);
                        _unitOfWork.CommonPlantRepository.PrepareUpdate(commonPlant);
                    }
                }
                else if (detail.NurseryMaterialId.HasValue)
                {
                    var nurseryMaterial = await _unitOfWork.NurseryMaterialRepository.GetByIdAsync(detail.NurseryMaterialId.Value);
                    if (nurseryMaterial != null)
                    {
                        nurseryMaterial.ReservedQuantity = Math.Max(0, nurseryMaterial.ReservedQuantity - quantity);
                        _unitOfWork.NurseryMaterialRepository.PrepareUpdate(nurseryMaterial);
                    }
                }

                if (detail.PlantInstanceId.HasValue)
                {
                    var plantInstance = await _unitOfWork.PlantInstanceRepository.GetByIdAsync(detail.PlantInstanceId.Value);
                    if (plantInstance != null && plantInstance.Status == (int)PlantInstanceStatusEnum.Reserved)
                    {
                        plantInstance.Status = (int)PlantInstanceStatusEnum.Sold;
                        _unitOfWork.PlantInstanceRepository.PrepareUpdate(plantInstance);
                    }
                }
            }
        }

    }
}
