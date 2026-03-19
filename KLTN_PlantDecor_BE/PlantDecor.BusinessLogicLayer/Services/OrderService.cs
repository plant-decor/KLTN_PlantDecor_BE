using Hangfire;
using Microsoft.EntityFrameworkCore;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly PlantDecorContext _context;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private const decimal DepositRatio = 0.3m;

        public OrderService(IUnitOfWork unitOfWork, PlantDecorContext context, IBackgroundJobClient backgroundJobClient)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _backgroundJobClient = backgroundJobClient;
        }

        public async Task<OrderResponseDto> CreateOrderAsync(int userId, CreateOrderRequestDto request)
        {
            var strategy = (PaymentStrategiesEnum)request.PaymentStrategy;
            var orderType = (OrderTypeEnum)request.OrderType;

            List<CreateOrderItemDto> orderItems;

            // Flow 1: PlantInstance Order (Buy Now - from request)
            if (orderType == OrderTypeEnum.PlantInstance)
            {
                if (!request.Items.Any())
                    throw new BadRequestException("PlantInstance order must have at least one item");

                if (request.Items.Count != 1)
                    throw new BadRequestException("PlantInstance order must have exactly one item");

                if (!request.Items[0].PlantInstanceId.HasValue)
                    throw new BadRequestException("PlantInstance order must contain a PlantInstance item");

                orderItems = request.Items;
            }
            // Flow 2: OtherProducts Order (From Cart)
            else
            {
                // Get cart items from database
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.CommonPlant)
                            .ThenInclude(cp => cp.Plant)
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.NurseryPlantCombo)
                            .ThenInclude(npc => npc.PlantCombo)
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.NurseryMaterial)
                            .ThenInclude(nm => nm.Material)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                    throw new BadRequestException("Cart is empty. Please add items to cart before creating order.");

                // Convert CartItems to CreateOrderItemDto
                orderItems = cart.CartItems.Select(ci => new CreateOrderItemDto
                {
                    CommonPlantId = ci.CommonPlantId,
                    NurseryPlantComboId = ci.NurseryPlantComboId,
                    NurseryMaterialId = ci.NurseryMaterialId,
                    ItemName = ci.CommonPlant?.Plant?.Name
                        ?? ci.NurseryPlantCombo?.PlantCombo?.ComboName
                        ?? ci.NurseryMaterial?.Material?.Name,
                    Quantity = ci.Quantity ?? 0,
                    Price = ci.Price ?? 0
                }).ToList();
            }

            // Validate payment strategy
            if (orderType != OrderTypeEnum.PlantInstance && strategy == PaymentStrategiesEnum.Deposit)
                throw new BadRequestException("Deposit payment strategy is only available for PlantInstance orders");

            // Resolve nursery for each item
            var itemsWithNursery = new List<(CreateOrderItemDto Item, int NurseryId)>();
            foreach (var item in orderItems)
            {
                var nurseryId = await ResolveNurseryIdForItemAsync(item);
                itemsWithNursery.Add((item, nurseryId));
            }

            // Group items by nursery
            var groupedItems = itemsWithNursery
                .GroupBy(x => x.NurseryId)
                .ToList();

            // Build a single order with multiple nursery orders
            var order = BuildOrder(userId, request, groupedItems);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Clear cart after successful order creation (only for OtherProducts)
            if (orderType != OrderTypeEnum.PlantInstance)
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart != null)
                {
                    _context.CartItems.RemoveRange(cart.CartItems);
                    await _context.SaveChangesAsync();
                }
            }

            // Hydrate the created order
            var hydratedOrder = await _context.Orders
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                .Include(o => o.Invoices)
                    .ThenInclude(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            return MapToDto(hydratedOrder!);
        }

        public async Task<OrderResponseDto> GetOrderByIdAsync(int orderId, int userId)
        {
            var order = await _context.Orders
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                .Include(o => o.Invoices)
                    .ThenInclude(i => i.InvoiceDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new NotFoundException($"Order {orderId} not found");

            if (order.UserId != userId)
                throw new ForbiddenException("You don't have access to this order");

            return MapToDto(order);
        }

        public async Task<List<OrderResponseDto>> GetMyOrdersAsync(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                .Include(o => o.Invoices)
                    .ThenInclude(i => i.InvoiceDetails)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return orders.Select(MapToDto).ToList();
        }

        public async Task<OrderResponseDto> CancelOrderAsync(int orderId, int userId)
        {
            var order = await _context.Orders
                .Include(o => o.Invoices)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new NotFoundException($"Order {orderId} not found");

            if (order.UserId != userId)
                throw new ForbiddenException("You don't have access to this order");

            var cancellableStatuses = new[] { (int)OrderStatusEnum.Pending, (int)OrderStatusEnum.DepositPaid };
            if (!cancellableStatuses.Contains(order.Status ?? -1))
                throw new BadRequestException("Order cannot be cancelled in its current status");

            order.Status = (int)OrderStatusEnum.Cancelled;
            order.UpdatedAt = DateTime.Now;

            foreach (var inv in order.Invoices.Where(i => i.Status == (int)InvoiceStatusEnum.Pending))
                inv.Status = (int)InvoiceStatusEnum.Cancelled;

            foreach (var nurseryOrder in order.NurseryOrders)
            {
                // Update NurseryOrder status to Cancelled
                nurseryOrder.Status = (int)OrderStatusEnum.Cancelled;
                nurseryOrder.UpdatedAt = DateTime.Now;

                // Update NurseryOrderDetails status to Cancelled (except already delivered items)
                foreach (var detail in nurseryOrder.NurseryOrderDetails
                             .Where(d => d.Status != (int)OrderItemStatusEnum.Delivered))
                {
                    detail.Status = (int)OrderItemStatusEnum.Cancelled;
                }
            }

            await _context.SaveChangesAsync();
            return MapToDto(order);
        }

        public async Task<OrderResponseDto> MarkOrderAsDeliveredAsync(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.Invoices)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                throw new NotFoundException($"Order {orderId} not found");

            // Validate order can be marked as delivered
            var deliverableStatuses = new[] { (int)OrderStatusEnum.Paid, (int)OrderStatusEnum.DepositPaid, (int)OrderStatusEnum.Shipping };
            if (!deliverableStatuses.Contains(order.Status ?? -1))
                throw new BadRequestException($"Order cannot be marked as delivered in its current status: {order.Status}");

            // Update Order status to Delivered
            order.Status = (int)OrderStatusEnum.Delivered;
            order.UpdatedAt = DateTime.Now;

            // Update all NurseryOrder status to match parent Order status
            foreach (var nurseryOrder in order.NurseryOrders)
            {
                nurseryOrder.Status = (int)OrderStatusEnum.Delivered;
                nurseryOrder.UpdatedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();

            // Enqueue background job to process order delivery (check strategy and create RemainingBalance invoice if needed)
            _backgroundJobClient.Enqueue<IOrderBackgroundJobService>(
                service => service.ProcessOrderDeliveryAsync(orderId));

            return MapToDto(order);
        }

        #region Helpers

        private async Task<int> ResolveNurseryIdForItemAsync(CreateOrderItemDto item)
        {
            var referenceCount = 0;
            if (item.CommonPlantId.HasValue) referenceCount++;
            if (item.PlantInstanceId.HasValue) referenceCount++;
            if (item.NurseryPlantComboId.HasValue) referenceCount++;
            if (item.NurseryMaterialId.HasValue) referenceCount++;

            if (referenceCount != 1)
                throw new BadRequestException("Each order item must reference exactly one product source");

            if (item.CommonPlantId.HasValue)
            {
                var commonPlant = await _unitOfWork.CommonPlantRepository.GetByIdAsync(item.CommonPlantId.Value)
                    ?? throw new NotFoundException($"CommonPlant {item.CommonPlantId.Value} not found");
                return commonPlant.NurseryId;
            }

            if (item.PlantInstanceId.HasValue)
            {
                var plantInstance = await _unitOfWork.PlantInstanceRepository.GetByIdAsync(item.PlantInstanceId.Value)
                    ?? throw new NotFoundException($"PlantInstance {item.PlantInstanceId.Value} not found");

                if (!plantInstance.CurrentNurseryId.HasValue)
                    throw new BadRequestException($"PlantInstance {item.PlantInstanceId.Value} does not belong to a nursery");

                return plantInstance.CurrentNurseryId.Value;
            }

            if (item.NurseryPlantComboId.HasValue)
            {
                var nurseryPlantCombo = await _unitOfWork.NurseryPlantComboRepository.GetByIdAsync(item.NurseryPlantComboId.Value)
                    ?? throw new NotFoundException($"NurseryPlantCombo {item.NurseryPlantComboId.Value} not found");
                return nurseryPlantCombo.NurseryId;
            }

            if (item.NurseryMaterialId.HasValue)
            {
                var nurseryMaterial = await _unitOfWork.NurseryMaterialRepository.GetByIdAsync(item.NurseryMaterialId.Value)
                    ?? throw new NotFoundException($"NurseryMaterial {item.NurseryMaterialId.Value} not found");
                return nurseryMaterial.NurseryId;
            }

            throw new BadRequestException("Invalid order item source");
        }

        private static Order BuildOrder(int userId, CreateOrderRequestDto request, List<IGrouping<int, (CreateOrderItemDto Item, int NurseryId)>> groupedItems)
        {
            var strategy = (PaymentStrategiesEnum)request.PaymentStrategy;
            var nurseryOrders = new List<NurseryOrder>();
            var allInvoiceDetails = new List<InvoiceDetail>();

            // Build NurseryOrder for each nursery group
            foreach (var group in groupedItems)
            {
                var nurseryId = group.Key;
                var items = group.Select(x => x.Item).ToList();
                var subTotalAmount = items.Sum(i => i.Price * i.Quantity);

                if (subTotalAmount <= 0)
                    throw new BadRequestException($"SubTotal amount for nursery {nurseryId} must be greater than 0");

                decimal? depositAmount = null;
                decimal? remainingAmount = null;
                if (strategy == PaymentStrategiesEnum.Deposit)
                {
                    depositAmount = Math.Round(subTotalAmount * DepositRatio, 2);
                    remainingAmount = subTotalAmount - depositAmount;
                }

                var nurseryOrderDetails = items.Select(i => new NurseryOrderDetail
                {
                    CommonPlantId = i.CommonPlantId,
                    PlantInstanceId = i.PlantInstanceId,
                    NurseryPlantComboId = i.NurseryPlantComboId,
                    NurseryMaterialId = i.NurseryMaterialId,
                    ItemName = i.ItemName,
                    Quantity = i.Quantity,
                    UnitPrice = i.Price,
                    Amount = i.Price * i.Quantity,
                    Status = (int)OrderItemStatusEnum.Pending
                }).ToList();

                // Add invoice details for Order invoice
                allInvoiceDetails.AddRange(nurseryOrderDetails.Select(d => new InvoiceDetail
                {
                    ItemName = d.ItemName,
                    UnitPrice = d.UnitPrice,
                    Quantity = d.Quantity,
                    Amount = d.Amount
                }));

                var nurseryOrder = new NurseryOrder
                {
                    NurseryId = nurseryId,
                    SubTotalAmount = subTotalAmount,
                    DepositAmount = depositAmount,
                    RemainingAmount = remainingAmount,
                    PaymentStrategy = request.PaymentStrategy,
                    Status = (int)OrderStatusEnum.Pending,
                    Note = request.Note,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    NurseryOrderDetails = nurseryOrderDetails
                };

                nurseryOrders.Add(nurseryOrder);
            }

            // Calculate Order totals
            var totalAmount = nurseryOrders.Sum(no => no.SubTotalAmount ?? 0);
            var totalDepositAmount = nurseryOrders.Sum(no => no.DepositAmount ?? 0);
            var totalRemainingAmount = nurseryOrders.Sum(no => no.RemainingAmount ?? 0);

            // Determine primary nursery (nursery with highest subtotal)
            var primaryNurseryId = nurseryOrders
                .OrderByDescending(no => no.SubTotalAmount)
                .First()
                .NurseryId;

            // Create single Invoice for Order (customer invoice)
            var invoiceType = strategy == PaymentStrategiesEnum.Deposit
                ? InvoiceTypeEnum.Deposit
                : InvoiceTypeEnum.FullPayment;

            var invoiceAmount = strategy == PaymentStrategiesEnum.Deposit ? totalDepositAmount : totalAmount;

            var orderInvoice = new Invoice
            {
                NurseryId = null, // Order invoice is for customer, not specific to one nursery
                Type = (int)invoiceType,
                TotalAmount = invoiceAmount,
                Status = (int)InvoiceStatusEnum.Pending,
                IssuedDate = DateTime.Now,
                InvoiceDetails = allInvoiceDetails
            };

            return new Order
            {
                UserId = userId,
                NurseryId = primaryNurseryId,
                Address = request.Address,
                Phone = request.Phone,
                CustomerName = request.CustomerName,
                Note = request.Note,
                TotalAmount = totalAmount,
                DepositAmount = strategy == PaymentStrategiesEnum.Deposit ? totalDepositAmount : null,
                RemainingAmount = strategy == PaymentStrategiesEnum.Deposit ? totalRemainingAmount : null,
                PaymentStrategy = request.PaymentStrategy,
                OrderType = request.OrderType,
                Status = (int)OrderStatusEnum.Pending,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                NurseryOrders = nurseryOrders,
                Invoices = new List<Invoice> { orderInvoice }
            };
        }

        private static OrderResponseDto MapToDto(Order order) => new()
        {
            Id = order.Id,
            UserId = order.UserId,
            NurseryId = order.NurseryId,
            Address = order.Address,
            Phone = order.Phone,
            CustomerName = order.CustomerName,
            TotalAmount = order.TotalAmount,
            DepositAmount = order.DepositAmount,
            RemainingAmount = order.RemainingAmount,
            Status = order.Status,
            StatusName = order.Status.HasValue ? ((OrderStatusEnum)order.Status.Value).ToString() : null,
            PaymentStrategy = order.PaymentStrategy,
            OrderType = order.OrderType,
            Note = order.Note,
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.NurseryOrders
                .SelectMany(no => no.NurseryOrderDetails)
                .Select(i => new OrderItemResponseDto
            {
                Id = i.Id,
                ItemName = i.ItemName,
                Quantity = i.Quantity,
                Price = i.UnitPrice,
                Status = i.Status,
                StatusName = i.Status.HasValue ? ((OrderItemStatusEnum)i.Status.Value).ToString() : null
            }).ToList(),
            Invoices = order.Invoices.Select(inv => new InvoiceResponseDto
            {
                Id = inv.Id,
                OrderId = inv.OrderId,
                NurseryId = inv.NurseryId,
                IssuedDate = inv.IssuedDate,
                TotalAmount = inv.TotalAmount,
                Type = inv.Type,
                TypeName = inv.Type.HasValue ? ((InvoiceTypeEnum)inv.Type.Value).ToString() : null,
                Status = inv.Status,
                StatusName = inv.Status.HasValue ? ((InvoiceStatusEnum)inv.Status.Value).ToString() : null,
                Details = inv.InvoiceDetails.Select(d => new InvoiceDetailResponseDto
                {
                    Id = d.Id,
                    ItemName = d.ItemName,
                    UnitPrice = d.UnitPrice,
                    Quantity = d.Quantity,
                    Amount = d.Amount
                }).ToList()
            }).ToList()
        };

        #endregion
    }
}
