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
        private const decimal DepositRatio = 0.3m;

        public OrderService(IUnitOfWork unitOfWork, PlantDecorContext context)
        {
            _unitOfWork = unitOfWork;
            _context = context;
        }

        public async Task<List<OrderResponseDto>> CreateOrderAsync(int userId, CreateOrderRequestDto request)
        {
            if (!request.Items.Any())
                throw new BadRequestException("Order must have at least one item");

            var strategy = (PaymentStrategiesEnum)request.PaymentStrategy;

            if (strategy == PaymentStrategiesEnum.Deposit && request.OrderType != (int)OrderTypeEnum.PlantInstance)
                throw new BadRequestException("Deposit payment strategy is only available for PlantInstance orders");

            var itemsWithNursery = new List<(CreateOrderItemDto Item, int NurseryId)>();
            foreach (var item in request.Items)
            {
                var nurseryId = await ResolveNurseryIdForItemAsync(item);
                itemsWithNursery.Add((item, nurseryId));
            }

            var groupedItems = itemsWithNursery
                .GroupBy(x => x.NurseryId)
                .ToList();

            var createdOrders = new List<Order>();
            foreach (var group in groupedItems)
            {
                var order = BuildOrderForNursery(userId, request, group.Key, group.Select(x => x.Item).ToList());
                createdOrders.Add(order);
            }

            _context.Orders.AddRange(createdOrders);
            await _context.SaveChangesAsync();

            var createdOrderIds = createdOrders.Select(o => o.Id).ToList();
            var hydratedOrders = await _context.Orders
                .Include(o => o.NurseryOrders)
                    .ThenInclude(no => no.NurseryOrderDetails)
                .Include(o => o.Invoices)
                    .ThenInclude(i => i.InvoiceDetails)
                .Where(o => createdOrderIds.Contains(o.Id))
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return hydratedOrders.Select(MapToDto).ToList();
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
                foreach (var detail in nurseryOrder.NurseryOrderDetails
                             .Where(d => d.Status != (int)OrderItemStatusEnum.Delivered))
                {
                    detail.Status = (int)OrderItemStatusEnum.Cancelled;
                }
            }

            await _context.SaveChangesAsync();
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

        private static Order BuildOrderForNursery(int userId, CreateOrderRequestDto request, int nurseryId, List<CreateOrderItemDto> items)
        {
            var totalAmount = items.Sum(i => i.Price * i.Quantity);
            if (totalAmount <= 0)
                throw new BadRequestException("Total amount must be greater than 0");

            var strategy = (PaymentStrategiesEnum)request.PaymentStrategy;

            decimal? depositAmount = null;
            decimal? remainingAmount = null;
            if (strategy == PaymentStrategiesEnum.Deposit)
            {
                depositAmount = Math.Round(totalAmount * DepositRatio, 2);
                remainingAmount = totalAmount - depositAmount;
            }

            var invoiceType = strategy == PaymentStrategiesEnum.Deposit
                ? InvoiceTypeEnum.Deposit
                : InvoiceTypeEnum.FullPayment;

            var invoiceAmount = strategy == PaymentStrategiesEnum.Deposit ? depositAmount!.Value : totalAmount;

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

            var invoice = new Invoice
            {
                NurseryId = nurseryId,
                Type = (int)invoiceType,
                TotalAmount = invoiceAmount,
                Status = (int)InvoiceStatusEnum.Pending,
                IssuedDate = DateTime.Now,
                InvoiceDetails = nurseryOrderDetails.Select(d => new InvoiceDetail
                {
                    ItemName = d.ItemName,
                    UnitPrice = d.UnitPrice,
                    Quantity = d.Quantity,
                    Amount = d.Amount
                }).ToList()
            };

            var nurseryOrder = new NurseryOrder
            {
                NurseryId = nurseryId,
                SubTotalAmount = totalAmount,
                DepositAmount = depositAmount,
                RemainingAmount = remainingAmount,
                PaymentStrategy = request.PaymentStrategy,
                Status = (int)OrderStatusEnum.Pending,
                Note = request.Note,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                NurseryOrderDetails = nurseryOrderDetails,
                Invoices = new List<Invoice> { invoice }
            };

            return new Order
            {
                UserId = userId,
                NurseryId = nurseryId,
                Address = request.Address,
                Phone = request.Phone,
                CustomerName = request.CustomerName,
                Note = request.Note,
                TotalAmount = totalAmount,
                DepositAmount = depositAmount,
                RemainingAmount = remainingAmount,
                PaymentStrategy = request.PaymentStrategy,
                OrderType = request.OrderType,
                Status = (int)OrderStatusEnum.Pending,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                NurseryOrders = new List<NurseryOrder> { nurseryOrder },
                Invoices = new List<Invoice> { invoice }
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
