namespace PlantDecor.DataAccessLayer.Helpers
{
    public class NurseryRevenueAggregate
    {
        public int NurseryId { get; set; }
        public string NurseryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int TotalOrders { get; set; }
    }
}
