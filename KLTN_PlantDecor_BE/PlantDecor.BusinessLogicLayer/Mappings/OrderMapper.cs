using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class OrderMapper
    {
        public static OrderResponseDto ToResponse(this Order order) => new()
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

        public static List<OrderResponseDto> ToResponseList(this IEnumerable<Order> orders)
        {
            return orders.Select(o => o.ToResponse()).ToList();
        }
    }
}
