namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class UserPlantResponseDto
    {
        public int Id { get; set; }
        public int? PlantId { get; set; }
        public int? PlantInstanceId { get; set; }
        public string? PlantName { get; set; }
        public string? PlantSpecificName { get; set; }
        public string? PrimaryImageUrl { get; set; }
        public DateOnly? PurchaseDate { get; set; }
        public DateOnly? LastWateredDate { get; set; }
        public DateOnly? LastFertilizedDate { get; set; }
        public DateOnly? LastPrunedDate { get; set; }
        public string? Location { get; set; }
        public decimal? CurrentTrunkDiameter { get; set; }
        public decimal? CurrentHeight { get; set; }
        public string? HealthStatus { get; set; }
        public int? Age { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}