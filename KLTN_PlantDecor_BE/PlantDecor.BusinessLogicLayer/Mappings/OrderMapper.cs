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
                .Select(i => i.ToOrderItemResponse())
                .ToList(),
            NurseryOrders = order.NurseryOrders.Select(no => new NurseryOrderResponseDto
            {
                Id = no.Id,
                NurseryId = no.NurseryId,
                NurseryName = no.Nursery?.Name,
                ShipperId = no.ShipperId,
                ShipperName = no.Shipper?.Username ?? no.Shipper?.Email,
                SubTotalAmount = no.SubTotalAmount,
                Status = no.Status,
                StatusName = no.Status.HasValue ? ((OrderStatusEnum)no.Status.Value).ToString() : null,
                ShipperNote = no.ShipperNote,
                Items = no.NurseryOrderDetails
                    .Select(d => d.ToOrderItemResponse())
                    .ToList()
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

        public static OrderItemResponseDto ToOrderItemResponse(this NurseryOrderDetail detail) => new()
        {
            Id = detail.Id,
            ItemName = detail.ItemName,
            ImageUrl = ResolveItemImageUrl(detail),
            Quantity = detail.Quantity,
            Price = detail.UnitPrice,
            Status = detail.Status,
            StatusName = detail.Status.HasValue ? ((OrderStatusEnum)detail.Status.Value).ToString() : null
        };

        private static string? ResolveItemImageUrl(NurseryOrderDetail detail)
        {
            var commonPlantImage = detail.CommonPlant?.Plant?.PlantImages?
                .OrderByDescending(i => i.IsPrimary == true)
                .Select(i => i.ImageUrl)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
            if (!string.IsNullOrWhiteSpace(commonPlantImage))
                return commonPlantImage;

            var plantInstanceImage = detail.PlantInstance?.PlantImages?
                .OrderByDescending(i => i.IsPrimary == true)
                .Select(i => i.ImageUrl)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
            if (!string.IsNullOrWhiteSpace(plantInstanceImage))
                return plantInstanceImage;

            var plantInstanceFallbackImage = detail.PlantInstance?.Plant?.PlantImages?
                .OrderByDescending(i => i.IsPrimary == true)
                .Select(i => i.ImageUrl)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
            if (!string.IsNullOrWhiteSpace(plantInstanceFallbackImage))
                return plantInstanceFallbackImage;

            var plantComboImage = detail.NurseryPlantCombo?.PlantCombo?.PlantComboImages?
                .OrderByDescending(i => i.IsPrimary == true)
                .Select(i => i.ImageUrl)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
            if (!string.IsNullOrWhiteSpace(plantComboImage))
                return plantComboImage;

            return detail.NurseryMaterial?.Material?.MaterialImages?
                .OrderByDescending(i => i.IsPrimary == true)
                .Select(i => i.ImageUrl)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
        }
    }
}
