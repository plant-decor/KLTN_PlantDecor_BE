namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public int NurseryId { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? CustomerName { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? DepositAmount { get; set; }
        public decimal? RemainingAmount { get; set; }
        public int? Status { get; set; }
        public string? StatusName { get; set; }
        public int? PaymentStrategy { get; set; }
        public string? OrderGroupCode { get; set; }
        public int? OrderType { get; set; }
        public string? Note { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
        public List<ShippingResponseDto> Shippings { get; set; } = new();
        public List<InvoiceResponseDto> Invoices { get; set; } = new();
    }

    public class OrderItemResponseDto
    {
        public int Id { get; set; }
        public string? ItemName { get; set; }
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
        public int? Status { get; set; }
        public string? StatusName { get; set; }
    }

    public class ShippingResponseDto
    {
        public int Id { get; set; }
        public int? Status { get; set; }
        public string? StatusName { get; set; }
        public string? TrackingCode { get; set; }
        public string? Note { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
