namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class PlantGuideResponseDto
    {
        public int Id { get; set; }
        public int PlantId { get; set; }
        public string? PlantName { get; set; }
        public int? LightRequirement { get; set; }
        public string? LightRequirementName { get; set; }
        public string? Watering { get; set; }
        public string? Fertilizing { get; set; }
        public string? Pruning { get; set; }
        public string? Temperature { get; set; }
        public string? Humidity { get; set; }
        public string? Soil { get; set; }
        public string? CareNotes { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}
