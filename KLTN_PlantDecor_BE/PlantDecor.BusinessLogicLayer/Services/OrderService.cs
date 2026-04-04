using Hangfire;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ICacheService _cacheService;
        private const decimal DepositRatio = 0.3m;
        private const string ALL_CART_KEY = "cart_user";

        public OrderService(IUnitOfWork unitOfWork, IBackgroundJobClient backgroundJobClient, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
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
                var plantInstance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(request.PlantInstanceId.Value);

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
                var cart = await _unitOfWork.CartRepository.GetByUserIdAsync(userId);

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

            _unitOfWork.OrderRepository.PrepareCreate(order);
            await _unitOfWork.SaveAsync();

            // Clear checked out cart items (only for OtherProducts)
            if (orderType != OrderTypeEnum.PlantInstance)
            {
                var cart = await _unitOfWork.CartRepository.GetByUserIdAsync(userId);

                if (cart != null)
                {
                    if (request.CartItemIds != null && request.CartItemIds.Any())
                    {
                        // Only remove selected items
                        var itemsToRemove = cart.CartItems
                            .Where(ci => request.CartItemIds.Contains(ci.Id))
                            .ToList();

                        foreach (var item in itemsToRemove)
                        {
                            await _unitOfWork.CartRepository.RemoveCartItemAsync(item);
                        }
                    }
                    else
                    {
                        await _unitOfWork.CartRepository.ClearCartItemsAsync(cart.Id);
                    }

                    // Invalidate cart cache in Redis so subsequent reads reflect DB changes
                    await _cacheService.RemoveByPrefixAsync($"{ALL_CART_KEY}_{userId}");
                }
            }

            // Hydrate the created order
            var hydratedOrder = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(order.Id);

            return hydratedOrder!.ToResponse();
        }

        public async Task<OrderResponseDto> GetOrderByIdAsync(int orderId, int userId)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId);

            if (order == null)
                throw new NotFoundException($"Order {orderId} not found");

            if (order.UserId != userId)
                throw new ForbiddenException("You don't have access to this order");

            return order.ToResponse();
        }

        public async Task<List<OrderResponseDto>> GetMyOrdersAsync(int userId, OrderStatusEnum? orderStatus = null)
        {
            var orders = await _unitOfWork.OrderRepository.GetByUserIdWithDetailsAsync(
                userId,
                orderStatus.HasValue ? (int)orderStatus.Value : null);

            return orders.ToResponseList();
        }

        public async Task<OrderResponseDto> CancelOrderAsync(int orderId, int userId)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId);

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

            _unitOfWork.OrderRepository.PrepareUpdate(order);
            await _unitOfWork.SaveAsync();
            return order.ToResponse();
        }

        public async Task<OrderResponseDto> MarkOrderAsDeliveredAsync(int orderId)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId);

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
                            var plantInstance = await _unitOfWork.PlantInstanceRepository
                                .GetByIdAsync(detail.PlantInstanceId.Value);

                            if (plantInstance != null && plantInstance.Status == (int)PlantInstanceStatusEnum.Reserved)
                            {
                                plantInstance.Status = (int)PlantInstanceStatusEnum.Sold;
                                _unitOfWork.PlantInstanceRepository.PrepareUpdate(plantInstance);
                            }
                        }
                    }
                }
            }

            _unitOfWork.OrderRepository.PrepareUpdate(order);
            await _unitOfWork.SaveAsync();

            // Enqueue background job to process order delivery (check strategy and create RemainingBalance invoice if needed)
            _backgroundJobClient.Enqueue<IOrderBackgroundJobService>(
                service => service.ProcessOrderDeliveryAsync(orderId));

            return order.ToResponse();
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

        #endregion
    }
}
