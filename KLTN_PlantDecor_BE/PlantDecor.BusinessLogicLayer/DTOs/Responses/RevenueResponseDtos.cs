namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class RevenueSummaryResponseDto
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
    }

    public class NurseryRevenueItemResponseDto
    {
        public int NurseryId { get; set; }
        public string NurseryName { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int TotalOrders { get; set; }
    }
}
