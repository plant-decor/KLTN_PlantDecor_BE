namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class CommonPlantResponseDto
    {
        public int Id { get; set; }
        public int PlantId { get; set; }
        public string? PlantName { get; set; }
        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
        public bool IsActive { get; set; }
        public int AvailableQuantity => Quantity - ReservedQuantity;
    }

    public class CommonPlantListResponseDto
    {
        public int Id { get; set; }
        public int PlantId { get; set; }
        public string? PlantName { get; set; }
        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
        public int Quantity { get; set; }
        public int ReservedQuantity { get; set; }
        public bool IsActive { get; set; }
        public int AvailableQuantity => Quantity - ReservedQuantity;
    }
}
