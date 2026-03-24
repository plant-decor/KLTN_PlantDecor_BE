namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class InvoiceResponseDto
    {
        public int Id { get; set; }
        public int? OrderId { get; set; }
        public DateTime? IssuedDate { get; set; }
        public decimal? TotalAmount { get; set; }
        public int? Type { get; set; }
        public string? TypeName { get; set; }
        public int? Status { get; set; }
        public string? StatusName { get; set; }
        public List<InvoiceDetailResponseDto> Details { get; set; } = new();
    }

    public class InvoiceDetailResponseDto
    {
        public int Id { get; set; }
        public string? ItemName { get; set; }
        public decimal? UnitPrice { get; set; }
        public int? Quantity { get; set; }
        public decimal? Amount { get; set; }
    }
}
