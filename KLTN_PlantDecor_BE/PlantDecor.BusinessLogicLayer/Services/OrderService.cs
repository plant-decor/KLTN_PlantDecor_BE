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
        private readonly ICacheService _cacheService;
        private const decimal DepositRatio = 0.3m;
        private const string ALL_CART_KEY = "cart_user";

        public OrderService(IUnitOfWork unitOfWork, PlantDecorContext context, IBackgroundJobClient backgroundJobClient, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _context = context;
            _backgroundJobClient = backgroundJobClient;
            _cacheService = cacheService;
        }

        public async Task<OrderResponseDto> CreateOrderAsync(int userId, CreateOrderRequestDto request)
        {
            var strategy = (PaymentStrategiesEnum)request.PaymentStrategy;
            var orderType = (OrderTypeEnum)request.OrderType;

            List<OrderItemInfo> orderItems;

            // Flow 1: PlantInstance Order (Buy Now - không qua Cart)
            if (orderType == OrderTypeEnum.PlantInstance)
            {
                if (!request.PlantInstanceId.HasValue)
                    throw new BadRequestException("PlantInstanceId is required for PlantInstance order");

                // Lấy thông tin PlantInstance từ DB - KHÔNG TIN CLIENT
                var plantInstance = await _context.PlantInstances
                    .Include(pi => pi.Plant)
                    .FirstOrDefaultAsync(pi => pi.Id == request.PlantInstanceId.Value);

                if (plantInstance == null)
                    throw new NotFoundException($"PlantInstance {request.PlantInstanceId.Value} not found");

                if (!plantInstance.CurrentNurseryId.HasValue)
                    throw new BadRequestException($"PlantInstance {request.PlantInstanceId.Value} does not belong to a nursery");

                if (plantInstance.Status != (int)PlantInstanceStatusEnum.Available)
                    throw new BadRequestException($"PlantInstance {request.PlantInstanceId.Value} is not available for purchase");

                orderItems = new List<OrderItemInfo>
                {
                    new()
                    {
                        PlantInstanceId = plantInstance.Id,
                        NurseryId = plantInstance.CurrentNurseryId.Value,
                        ItemName = plantInstance.Plant?.Name ?? $"PlantInstance #{plantInstance.Id}",
                        Quantity = 1, // PlantInstance luôn là 1
                        Price = plantInstance.SpecificPrice ?? 0
                    }
                };
            }
            // Flow 2: OtherProducts Order (From Cart)
            else
            {
                // Get cart with items from database
                var cartQuery = _context.Carts
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.CommonPlant)
                            .ThenInclude(cp => cp!.Plant)
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.NurseryPlantCombo)
                            .ThenInclude(npc => npc!.PlantCombo)
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.NurseryMaterial)
                            .ThenInclude(nm => nm!.Material)
                    .Where(c => c.UserId == userId);

                var cart = await cartQuery.FirstOrDefaultAsync();

                if (cart == null || !cart.CartItems.Any())
                    throw new BadRequestException("Cart is empty. Please add items to cart before creating order.");

                // Xác định những CartItem nào sẽ checkout
                IEnumerable<CartItem> cartItemsToCheckout;

                if (request.CartItemIds != null && request.CartItemIds.Any())
                {
                    // Checkout selected items only
                    cartItemsToCheckout = cart.CartItems
                        .Where(ci => request.CartItemIds.Contains(ci.Id))
                        .ToList();

                    if (!cartItemsToCheckout.Any())
                        throw new BadRequestException("No valid cart items found for the given CartItemIds");

                    // Validate all requested CartItemIds exist
                    var foundIds = cartItemsToCheckout.Select(ci => ci.Id).ToHashSet();
                    var missingIds = request.CartItemIds.Where(id => !foundIds.Contains(id)).ToList();
                    if (missingIds.Any())
                        throw new BadRequestException($"CartItem(s) not found: {string.Join(", ", missingIds)}");
                }
                else
                {
                    // Checkout all items in cart
                    cartItemsToCheckout = cart.CartItems;
                }

                // Convert CartItems to OrderItemInfo - LẤY GIÁ TỪ DB
                orderItems = cartItemsToCheckout.Select(ci => new OrderItemInfo
                {
                    CommonPlantId = ci.CommonPlantId,
                    NurseryPlantComboId = ci.NurseryPlantComboId,
                    NurseryMaterialId = ci.NurseryMaterialId,
                    NurseryId = ResolveNurseryIdFromCartItem(ci),
                    ItemName = ci.CommonPlant?.Plant?.Name
                        ?? ci.NurseryPlantCombo?.PlantCombo?.ComboName
                        ?? ci.NurseryMaterial?.Material?.Name
                        ?? "Unknown Item",
                    Quantity = ci.Quantity ?? 0,
                    // LẤY GIÁ TỪ DB - KHÔNG TIN CLIENT
                    Price = GetPriceFromCartItem(ci)
                }).ToList();
            }

            // Validate payment strategy
            if (orderType != OrderTypeEnum.PlantInstance && strategy == PaymentStrategiesEnum.Deposit)
                throw new BadRequestException("Deposit payment strategy is only available for PlantInstance orders");

            // Group items by nursery
            var groupedItems = orderItems
                .GroupBy(x => x.NurseryId)
                .ToList();

            // Build a single order with multiple nursery orders
            var order = BuildOrder(userId, request, groupedItems);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Clear checked out cart items (only for OtherProducts)
            if (orderType != OrderTypeEnum.PlantInstance)
            {
                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart != null)
                {
                    IEnumerable<CartItem> itemsToRemove;

                    if (request.CartItemIds != null && request.CartItemIds.Any())
                    {
                        // Only remove selected items
                        itemsToRemove = cart.CartItems.Where(ci => request.CartItemIds.Contains(ci.Id));
                    }
                    else
                    {
                        // Remove all items
                        itemsToRemove = cart.CartItems;
                    }

                    _context.CartItems.RemoveRange(itemsToRemove);
                    await _context.SaveChangesAsync();

                    // Invalidate cart cache in Redis so subsequent reads reflect DB changes
                    await _cacheService.RemoveByPrefixAsync($"{ALL_CART_KEY}_{userId}");
                }
            }

            // Hydrate the created order
            var hydratedOrder = await _context.Orders
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.Nursery)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.Shipper)
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
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.Nursery)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.Shipper)
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
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.Nursery)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.Shipper)
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
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.Nursery)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.Shipper)
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
                             .Where(d => d.Status != (int)NurseryOrderStatus.Delivered))
                {
                    detail.Status = (int)NurseryOrderStatus.Cancelled;
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
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.Nursery)
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.Shipper)
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
                nurseryOrder.Status = (int)NurseryOrderStatus.Delivered;
                nurseryOrder.UpdatedAt = DateTime.Now;
            }

            // Update PlantInstance status to Sold for PlantInstance orders
            if (order.OrderType == (int)OrderTypeEnum.PlantInstance)
            {
                foreach (var nurseryOrder in order.NurseryOrders)
                {
                    foreach (var detail in nurseryOrder.NurseryOrderDetails)
                    {
                        if (detail.PlantInstanceId.HasValue)
                        {
                            var plantInstance = await _context.PlantInstances
                                .FirstOrDefaultAsync(pi => pi.Id == detail.PlantInstanceId.Value);

                            if (plantInstance != null && plantInstance.Status == (int)PlantInstanceStatusEnum.Reserved)
                            {
                                plantInstance.Status = (int)PlantInstanceStatusEnum.Sold;
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            // Enqueue background job to process order delivery (check strategy and create RemainingBalance invoice if needed)
            _backgroundJobClient.Enqueue<IOrderBackgroundJobService>(
                service => service.ProcessOrderDeliveryAsync(orderId));

            return MapToDto(order);
        }

        #region Helpers

        /// <summary>
        /// Internal class để chứa thông tin item đã được validate và lấy giá từ DB
        /// </summary>
        private class OrderItemInfo
        {
            public int? CommonPlantId { get; set; }
            public int? PlantInstanceId { get; set; }
            public int? NurseryPlantComboId { get; set; }
            public int? NurseryMaterialId { get; set; }
            public int NurseryId { get; set; }
            public string ItemName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public decimal Price { get; set; }
        }

        /// <summary>
        /// Lấy NurseryId từ CartItem dựa trên product type
        /// </summary>
        private static int ResolveNurseryIdFromCartItem(CartItem cartItem)
        {
            if (cartItem.CommonPlant != null)
                return cartItem.CommonPlant.NurseryId;

            if (cartItem.NurseryPlantCombo != null)
                return cartItem.NurseryPlantCombo.NurseryId;

            if (cartItem.NurseryMaterial != null)
                return cartItem.NurseryMaterial.NurseryId;

            throw new BadRequestException($"CartItem {cartItem.Id} does not reference any valid product");
        }

        /// <summary>
        /// Lấy giá từ DB dựa trên product type - KHÔNG TIN CLIENT
        /// </summary>
        private static decimal GetPriceFromCartItem(CartItem cartItem)
        {
            if (cartItem.CommonPlant != null)
                return cartItem.CommonPlant.Plant?.BasePrice ?? 0;

            if (cartItem.NurseryPlantCombo != null)
                return cartItem.NurseryPlantCombo.PlantCombo?.ComboPrice ?? 0;

            if (cartItem.NurseryMaterial != null)
                return cartItem.NurseryMaterial.Material?.BasePrice ?? 0;

            return 0;
        }

        private static Order BuildOrder(int userId, CreateOrderRequestDto request, List<IGrouping<int, OrderItemInfo>> groupedItems)
        {
            var strategy = (PaymentStrategiesEnum)request.PaymentStrategy;
            var nurseryOrders = new List<NurseryOrder>();
            var allInvoiceDetails = new List<InvoiceDetail>();

            // Build NurseryOrder for each nursery group
            foreach (var group in groupedItems)
            {
                var nurseryId = group.Key;
                var items = group.ToList();
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
                    Status = (int)NurseryOrderStatus.Pending
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
                    Status = (int)NurseryOrderStatus.Pending,
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

            // Create single Invoice for Order (customer invoice)
            var invoiceType = strategy == PaymentStrategiesEnum.Deposit
                ? InvoiceTypeEnum.Deposit
                : InvoiceTypeEnum.FullPayment;

            var invoiceAmount = strategy == PaymentStrategiesEnum.Deposit ? totalDepositAmount : totalAmount;

            var orderInvoice = new Invoice
            {
                Type = (int)invoiceType,
                TotalAmount = invoiceAmount,
                Status = (int)InvoiceStatusEnum.Pending,
                IssuedDate = DateTime.Now,
                InvoiceDetails = allInvoiceDetails
            };

            return new Order
            {
                UserId = userId,
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
                    StatusName = i.Status.HasValue ? ((NurseryOrderStatus)i.Status.Value).ToString() : null
                }).ToList(),
            NurseryOrders = order.NurseryOrders.Select(no => new NurseryOrderResponseDto
            {
                Id = no.Id,
                NurseryId = no.NurseryId,
                NurseryName = no.Nursery?.Name,
                ShipperId = no.ShipperId,
                ShipperName = no.Shipper?.Username ?? no.Shipper?.Email,
                SubTotalAmount = no.SubTotalAmount,
                Status = no.Status,
                StatusName = no.Status.HasValue ? ((NurseryOrderStatus)no.Status.Value).ToString() : null,
                ShipperNote = no.ShipperNote,
                Items = no.NurseryOrderDetails.Select(d => new OrderItemResponseDto
                {
                    Id = d.Id,
                    ItemName = d.ItemName,
                    Quantity = d.Quantity,
                    Price = d.UnitPrice,
                    Status = d.Status,
                    StatusName = d.Status.HasValue ? ((NurseryOrderStatus)d.Status.Value).ToString() : null
                }).ToList()
            }).ToList(),
            Invoices = order.Invoices.Select(inv => new InvoiceResponseDto
            {
                Id = inv.Id,
                OrderId = inv.OrderId,
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
