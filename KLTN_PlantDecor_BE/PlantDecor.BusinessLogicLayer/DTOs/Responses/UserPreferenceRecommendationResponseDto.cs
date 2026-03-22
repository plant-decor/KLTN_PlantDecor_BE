namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class UserPreferenceRecommendationResponseDto
    {
        public int PlantId { get; set; }
        public string PlantName { get; set; } = null!;
        public string? PrimaryImageUrl { get; set; }
        public decimal? BasePrice { get; set; }
        public string? CareLevel { get; set; }
        public string? FengShuiElement { get; set; }

        public decimal PreferenceScore { get; set; }
        public decimal ProfileMatchScore { get; set; }
        public decimal BehaviorScore { get; set; }
        public decimal PurchaseHistoryScore { get; set; }
    }
}
