namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class OrderStatusSummaryItemDto
    {
        public int Status { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
    }

    public class OrderStatusSummaryResponseDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public List<OrderStatusSummaryItemDto> Items { get; set; } = new();
    }

    public class FailedOrderSummaryResponseDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public int TotalFailedOrders { get; set; }
    }

    public class TopProductResponseDto
    {
        public string ProductType { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class SystemLowStockProductAlertDto
    {
        public int NurseryId { get; set; }
        public string NurseryName { get; set; } = string.Empty;
        public string ProductType { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int TotalQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public int Threshold { get; set; }
    }
}
