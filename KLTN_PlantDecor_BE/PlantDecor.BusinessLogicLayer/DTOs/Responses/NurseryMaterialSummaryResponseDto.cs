namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class NurseryMaterialExpiryAlertDto
    {
        public int NurseryMaterialId { get; set; }
        public int MaterialId { get; set; }
        public string? MaterialName { get; set; }
        public string? MaterialCode { get; set; }
        public string? Unit { get; set; }
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity => Quantity;
        public DateOnly? ExpiredDate { get; set; }
        public int DaysToExpire { get; set; }
    }

    public class NurseryLowStockProductAlertDto
    {
        public string ProductType { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int TotalQuantity { get; set; }
        public int ReservedQuantity { get; set; }
        public int AvailableQuantity { get; set; }
        public int Threshold { get; set; }
    }

    public class NurseryMaterialSummaryResponseDto
    {
        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
        public DateTime GeneratedAt { get; set; }
        public NurseryCommonPlantSummaryDto CommonPlants { get; set; } = new();
        public NurseryPlantInstanceSummaryDto PlantInstances { get; set; } = new();
        public NurseryMaterialSummaryDto Materials { get; set; } = new();
    }

    public class NurseryCommonPlantSummaryDto
    {
        public int TotalProducts { get; set; }
        public int TotalQuantity { get; set; }
        public int TotalReservedQuantity { get; set; }
        public int TotalAvailableQuantity { get; set; }
        public int LowStockProducts { get; set; }
    }

    public class NurseryPlantInstanceSummaryDto
    {
        public int TotalInstances { get; set; }
        public int AvailableInstances { get; set; }
        public int ReservedInstances { get; set; }
        public int SoldInstances { get; set; }
        public int DamagedInstances { get; set; }
        public int InactiveInstances { get; set; }
        public int LowStockPlants { get; set; }
    }

    public class NurseryMaterialSummaryDto
    {
        public int TotalProducts { get; set; }
        public int TotalQuantity { get; set; }
        public int TotalReservedQuantity { get; set; }
        public int TotalAvailableQuantity { get; set; }
        public int ExpiringSoonProducts { get; set; }
        public int LowStockProducts { get; set; }
    }
}