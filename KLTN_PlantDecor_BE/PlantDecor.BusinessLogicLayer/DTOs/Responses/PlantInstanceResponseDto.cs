namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class PlantInstanceResponseDto
    {
        public int Id { get; set; }
        public int? PlantId { get; set; }
        public string? PlantName { get; set; }
        public decimal? SpecificPrice { get; set; }
        public decimal? Height { get; set; }
        public decimal? TrunkDiameter { get; set; }
        public string? HealthStatus { get; set; }
        public int? Age { get; set; }
        public string? Description { get; set; }
        public int Status { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
