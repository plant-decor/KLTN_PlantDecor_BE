namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class UpdateUserPlantRequestDto
    {
        public DateOnly? PurchaseDate { get; set; }
        public DateOnly? LastWateredDate { get; set; }
        public DateOnly? LastFertilizedDate { get; set; }
        public DateOnly? LastPrunedDate { get; set; }
        public string? Location { get; set; }
        public decimal? CurrentTrunkDiameter { get; set; }
        public decimal? CurrentHeight { get; set; }
        public string? HealthStatus { get; set; }
        public int? Age { get; set; }
    }
}
