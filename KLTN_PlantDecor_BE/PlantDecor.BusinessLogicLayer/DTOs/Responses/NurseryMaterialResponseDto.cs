namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class NurseryMaterialResponseDto
    {
        public int Id { get; set; }
        public int MaterialId { get; set; }
        public string? MaterialName { get; set; }
        public string? MaterialCode { get; set; }
        public string? Unit { get; set; }
        public decimal? BasePrice { get; set; }
        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
        public bool IsActive { get; set; }
        public int AvailableQuantity => Quantity - ReservedQuantity;
    }

    public class NurseryMaterialListResponseDto
    {
        public int Id { get; set; }
        public int MaterialId { get; set; }
        public string? MaterialName { get; set; }
        public string? MaterialCode { get; set; }
        public string? Unit { get; set; }
        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
        public bool IsActive { get; set; }
        public int AvailableQuantity => Quantity - ReservedQuantity;
    }
}
