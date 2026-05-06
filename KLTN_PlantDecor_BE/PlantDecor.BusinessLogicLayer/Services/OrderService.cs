using Hangfire;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ICacheService _cacheService;
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

            ValidateCreateOrderRequest(orderType, request);

            List<OrderItemInfo> orderItems;
            var isCheckoutFromCart = false;

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
            // Flow 2: OtherProducts Buy Now (khong qua Cart)
            else if (orderType == OrderTypeEnum.OtherProductBuyNow)
            {
                orderItems = await BuildOtherProductBuyNowOrderItemsAsync(request);
            }
            // Flow 3: OtherProducts Order (From Cart)
            else if (orderType == OrderTypeEnum.OtherProduct)
            {
                isCheckoutFromCart = true;

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
            else
            {
                throw new BadRequestException($"OrderType {request.OrderType} is not supported for create order");
            }

            // Validate payment strategy
            if (orderType != OrderTypeEnum.PlantInstance && strategy == PaymentStrategiesEnum.Deposit)
                throw new BadRequestException("Deposit payment strategy is only available for PlantInstance orders");

            decimal? depositRatio = null;
            if (strategy == PaymentStrategiesEnum.Deposit)
            {
                var plantInstanceItem = orderItems.FirstOrDefault(i => i.PlantInstanceId.HasValue);
                if (plantInstanceItem == null)
                    throw new BadRequestException("Unable to resolve PlantInstance item for deposit order");

                var matchedPolicy = await _unitOfWork.DepositPolicyRepository
                    .GetMatchingActivePolicyByPriceAsync(plantInstanceItem.Price);

                if (matchedPolicy == null)
                    throw new BadRequestException($"No active deposit policy matched plant price {plantInstanceItem.Price}");

                depositRatio = matchedPolicy.DepositPercentage / 100m;
            }

            // Group items by nursery
            var groupedItems = orderItems
                .GroupBy(x => x.NurseryId)
                .ToList();

            // Build a single order with multiple nursery orders
            var order = BuildOrder(userId, request, groupedItems, depositRatio);

            _unitOfWork.OrderRepository.PrepareCreate(order);
            await _unitOfWork.SaveAsync();

            // Clear checked out cart items (only for OtherProducts)
            if (isCheckoutFromCart)
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

        public async Task<PaginatedResult<OrderResponseDto>> GetDesignOrdersForOperatorAsync(
            int operatorId,
            Pagination pagination,
            OrderStatusEnum? status = null)
        {
            var nursery = await ResolveOperatorNurseryAsync(operatorId);
            var result = await _unitOfWork.OrderRepository.SearchDesignForOperatorAsync(
                nursery.Id,
                pagination,
                status.HasValue ? (int)status.Value : null);

            return new PaginatedResult<OrderResponseDto>(
                result.Items.ToResponseList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<List<OrderResponseDto>> GetOrdersByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new BadRequestException("Email is required");

            var user = await _unitOfWork.UserRepository.GetByEmailAsync(email);
            if (user == null)
                throw new NotFoundException($"User with email '{email}' not found");

            var orders = await _unitOfWork.OrderRepository.GetByUserIdWithDetailsAsync(user.Id);
            return orders.ToResponseList();
        }

        public async Task<PaginatedResult<OrderResponseDto>> GetOrdersForConsultantAsync(
            ConsultantOrderSearchRequestDto request,
            Pagination pagination)
        {
            var filter = request ?? new ConsultantOrderSearchRequestDto();
            var appliedPagination = pagination ?? new Pagination();

            if (filter.CreatedFrom.HasValue && filter.CreatedTo.HasValue
                && filter.CreatedFrom.Value > filter.CreatedTo.Value)
                throw new BadRequestException("CreatedFrom must be less than or equal to CreatedTo");

            if (filter.MinTotalAmount.HasValue && filter.MaxTotalAmount.HasValue
                && filter.MinTotalAmount.Value > filter.MaxTotalAmount.Value)
                throw new BadRequestException("MinTotalAmount must be less than or equal to MaxTotalAmount");

            var result = await _unitOfWork.OrderRepository.SearchForConsultantAsync(
                appliedPagination,
                filter.Status,
                filter.OrderType,
                filter.PaymentStrategy,
                filter.CreatedFrom,
                filter.CreatedTo,
                filter.MinTotalAmount,
                filter.MaxTotalAmount,
                filter.CustomerEmail,
                filter.SortBy,
                filter.SortDirection);

            return new PaginatedResult<OrderResponseDto>(
                result.Items.ToResponseList(),
                result.TotalCount,
                result.PageNumber,
                result.PageSize);
        }

        public async Task<OrderResponseDto> GetOrderByIdForConsultantAsync(int orderId)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId);

            if (order == null)
                throw new NotFoundException($"Order {orderId} not found");

            return order.ToResponse();
        }

        public async Task<OrderResponseDto> CancelOrderAsync(int orderId, int userId)
        {
            var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId);

            if (order == null)
                throw new NotFoundException($"Order {orderId} not found");

            if (order.UserId != userId)
                throw new ForbiddenException("You don't have access to this order");

            var cancellableStatuses = new[] { (int)OrderStatusEnum.Pending, (int)OrderStatusEnum.DepositPaid, (int)OrderStatusEnum.Paid };
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
                             .Where(d => d.Status != (int)OrderStatusEnum.Delivered))
                {
                    detail.Status = (int)OrderStatusEnum.Cancelled;
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
                nurseryOrder.Status = (int)OrderStatusEnum.Delivered;
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

        private async Task<Nursery> ResolveOperatorNurseryAsync(int operatorId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(operatorId);
            if (nursery != null)
                return nursery;

            var user = await _unitOfWork.UserRepository.GetByIdAsync(operatorId);
            if (user?.RoleId == (int)RoleEnum.Staff && user.NurseryId.HasValue)
            {
                var staffNursery = await _unitOfWork.NurseryRepository.GetByIdAsync(user.NurseryId.Value);
                if (staffNursery != null)
                    return staffNursery;
            }

            throw new ForbiddenException("You are not a manager/staff of any nursery");
        }

        private static void ValidateCreateOrderRequest(OrderTypeEnum orderType, CreateOrderRequestDto request)
        {
            if (!Enum.IsDefined(typeof(OrderTypeEnum), (int)orderType))
                throw new BadRequestException($"Invalid OrderType: {request.OrderType}");

            switch (orderType)
            {
                case OrderTypeEnum.PlantInstance:
                    if (request.CartItemIds != null && request.CartItemIds.Any())
                        throw new BadRequestException("CartItemIds are not allowed for PlantInstance orders");

                    if (request.BuyNowItemId.HasValue || request.BuyNowItemType.HasValue)
                        throw new BadRequestException("BuyNowItem fields are only allowed for OtherProductBuyNow orders");

                    break;

                case OrderTypeEnum.OtherProductBuyNow:
                    if (request.CartItemIds != null && request.CartItemIds.Any())
                        throw new BadRequestException("CartItemIds are not allowed for OtherProductBuyNow orders");

                    if (request.PlantInstanceId.HasValue)
                        throw new BadRequestException("PlantInstanceId is not allowed for OtherProductBuyNow orders");

                    break;

                case OrderTypeEnum.OtherProduct:
                    if (request.BuyNowItemId.HasValue || request.BuyNowItemType.HasValue)
                        throw new BadRequestException("BuyNowItem fields are not allowed when OrderType is OtherProduct. Use OrderType = OtherProductBuyNow");

                    if (request.PlantInstanceId.HasValue)
                        throw new BadRequestException("PlantInstanceId is only allowed for PlantInstance orders");

                    break;

                default:
                    throw new BadRequestException($"OrderType {request.OrderType} is not supported for create order");
            }
        }

        private async Task<List<OrderItemInfo>> BuildOtherProductBuyNowOrderItemsAsync(CreateOrderRequestDto request)
        {
            if (!request.BuyNowItemId.HasValue)
                throw new BadRequestException("BuyNowItemId is required for OtherProductBuyNow order");

            if (!request.BuyNowItemType.HasValue)
                throw new BadRequestException("BuyNowItemType is required for OtherProductBuyNow order");

            if (request.BuyNowQuantity <= 0)
                throw new BadRequestException("BuyNowQuantity must be greater than 0");

            var itemType = (BuyNowItemTypeEnum)request.BuyNowItemType.Value;
            var itemId = request.BuyNowItemId.Value;
            var quantity = request.BuyNowQuantity;

            var orderItem = itemType switch
            {
                BuyNowItemTypeEnum.CommonPlant => await BuildCommonPlantBuyNowItemAsync(itemId, quantity),
                BuyNowItemTypeEnum.NurseryPlantCombo => await BuildNurseryPlantComboBuyNowItemAsync(itemId, quantity),
                BuyNowItemTypeEnum.NurseryMaterial => await BuildNurseryMaterialBuyNowItemAsync(itemId, quantity),
                _ => throw new BadRequestException($"Unsupported BuyNowItemType: {request.BuyNowItemType.Value}")
            };

            return new List<OrderItemInfo> { orderItem };
        }

        private async Task<OrderItemInfo> BuildCommonPlantBuyNowItemAsync(int itemId, int quantity)
        {
            var commonPlant = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(itemId);
            if (commonPlant == null || !commonPlant.IsActive)
                throw new NotFoundException($"CommonPlant {itemId} not exists or has been discontinued");

            if (commonPlant.Quantity < quantity)
                throw new BadRequestException($"The remaining stock isn't enough for request. Remaining: {commonPlant.Quantity}");

            return new OrderItemInfo
            {
                CommonPlantId = commonPlant.Id,
                NurseryId = commonPlant.NurseryId,
                ItemName = commonPlant.Plant?.Name ?? $"CommonPlant #{commonPlant.Id}",
                Quantity = quantity,
                Price = commonPlant.Plant?.BasePrice ?? 0
            };
        }

        private async Task<OrderItemInfo> BuildNurseryMaterialBuyNowItemAsync(int itemId, int quantity)
        {
            var nurseryMaterial = await _unitOfWork.NurseryMaterialRepository.GetByIdWithDetailsAsync(itemId);
            if (nurseryMaterial == null || !nurseryMaterial.IsActive)
                throw new NotFoundException($"NurseryMaterial {itemId} not exists or has been discontinued");

            if (nurseryMaterial.Quantity < quantity)
                throw new BadRequestException($"The remaining stock isn't enough for request. Remaining: {nurseryMaterial.Quantity}");

            return new OrderItemInfo
            {
                NurseryMaterialId = nurseryMaterial.Id,
                NurseryId = nurseryMaterial.NurseryId,
                ItemName = nurseryMaterial.Material?.Name ?? $"NurseryMaterial #{nurseryMaterial.Id}",
                Quantity = quantity,
                Price = nurseryMaterial.Material?.BasePrice ?? 0
            };
        }

        private async Task<OrderItemInfo> BuildNurseryPlantComboBuyNowItemAsync(int itemId, int quantity)
        {
            var combo = await _unitOfWork.NurseryPlantComboRepository.GetByIdAsync(itemId);
            if (combo == null || !combo.IsActive)
                throw new NotFoundException($"NurseryPlantCombo {itemId} not exists or has been discontinued");

            if (combo.Quantity < quantity)
                throw new BadRequestException($"The remaining stock isn't enough for request. Remaining: {combo.Quantity}");

            return new OrderItemInfo
            {
                NurseryPlantComboId = combo.Id,
                NurseryId = combo.NurseryId,
                ItemName = combo.PlantCombo?.ComboName ?? $"NurseryPlantCombo #{combo.Id}",
                Quantity = quantity,
                Price = combo.PlantCombo?.ComboPrice ?? 0
            };
        }

        private static Order BuildOrder(
            int userId,
            CreateOrderRequestDto request,
            List<IGrouping<int, OrderItemInfo>> groupedItems,
            decimal? depositRatio = null)
        {
            var strategy = (PaymentStrategiesEnum)request.PaymentStrategy;

            if(strategy != PaymentStrategiesEnum.Deposit)
            {
                depositRatio = null; // Ensure depositRatio is null if strategy is not Deposit
            }

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
                    if (!depositRatio.HasValue || depositRatio.Value <= 0 || depositRatio.Value > 1)
                        throw new BadRequestException("Invalid deposit policy configuration");

                    depositAmount = Math.Round(subTotalAmount * depositRatio.Value, MidpointRounding.AwayFromZero);
                    remainingAmount = Math.Round(subTotalAmount - depositAmount.Value, MidpointRounding.AwayFromZero);
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
                    Status = (int)OrderStatusEnum.Pending
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
            var totalAmount = Math.Round(nurseryOrders.Sum(no => no.SubTotalAmount ?? 0), 2);
            var totalDepositAmount = Math.Round(nurseryOrders.Sum(no => no.DepositAmount ?? 0), 2);
            var totalRemainingAmount = Math.Round(nurseryOrders.Sum(no => no.RemainingAmount ?? 0), 2);

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
