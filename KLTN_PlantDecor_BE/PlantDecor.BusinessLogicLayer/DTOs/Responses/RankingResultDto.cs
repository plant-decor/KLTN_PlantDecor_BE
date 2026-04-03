namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class RankingResultDto
    {
        public string? EntityType { get; set; }
        public int EntityId { get; set; }
        public string? ReasonForRecommendation { get; set; }
        public string? SuggestedPlacement { get; set; }
        public double MatchScore { get; set; }
    }
}
