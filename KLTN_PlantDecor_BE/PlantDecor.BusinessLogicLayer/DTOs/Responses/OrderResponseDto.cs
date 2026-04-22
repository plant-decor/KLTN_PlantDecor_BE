namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class OrderResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        public string? CustomerName { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? DepositAmount { get; set; }
        public decimal? RemainingAmount { get; set; }
        public int? Status { get; set; }
        public string? StatusName { get; set; }
        public int? PaymentStrategy { get; set; }
        public int? OrderType { get; set; }
        public string? Note { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
        public List<NurseryOrderResponseDto> NurseryOrders { get; set; } = new();
        public List<InvoiceResponseDto> Invoices { get; set; } = new();
    }

    public class NurseryOrderResponseDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
        public int? ShipperId { get; set; }
        public string? ShipperName { get; set; }
        public string? ShipperEmail { get; set; }
        public string? ShipperPhone { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerAddress { get; set; }
        public decimal? SubTotalAmount { get; set; }
        public int? Status { get; set; }
        public string? StatusName { get; set; }
        public string? ShipperNote { get; set; }
        public string? DeliveryNote { get; set; }
        public string? DeliveryImageUrl { get; set; }
        public string? Note { get; set; }
        public List<OrderItemResponseDto> Items { get; set; } = new();
    }

    public class OrderItemResponseDto
    {
        public int Id { get; set; }
        public string? ItemName { get; set; }
        public string? ImageUrl { get; set; }
        public int? Quantity { get; set; }
        public decimal? Price { get; set; }
        public int? Status { get; set; }
        public string? StatusName { get; set; }
    }
}
