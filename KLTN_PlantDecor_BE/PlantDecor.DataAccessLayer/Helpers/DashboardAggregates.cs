namespace PlantDecor.DataAccessLayer.Helpers
{
    public class OrderStatusAggregate
    {
        public int Status { get; set; }
        public int TotalOrders { get; set; }
    }

    public class TopProductAggregate
    {
        public string ProductType { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
