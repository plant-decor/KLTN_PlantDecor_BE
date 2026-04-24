using System.Text.Json.Serialization;

namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class SemanticSearchResponseDto
    {
        public List<SearchResultItemDto> Results { get; set; } = new();
        public int TotalCount { get; set; }
        public string? Query { get; set; }
    }

    public class SearchResultItemDto
    {
        public string EntityType { get; set; } = null!;
        public int EntityId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
        public bool IsPurchasable { get; set; }
        public string? ImageUrl { get; set; }
        public double SimilarityScore { get; set; }
        public string? FengShuiElement { get; set; }
        public Dictionary<string, object>? AdditionalInfo { get; set; }

        [JsonIgnore]
        public bool? PetSafe { get; set; }

        [JsonIgnore]
        public bool? ChildSafe { get; set; }
    }

    public class RoomRecommendationResponseDto
    {
        public List<PlantRecommendationItemDto> Recommendations { get; set; } = new();
        public int TotalCount { get; set; }
        public string? RoomDescription { get; set; }
        public string? FengShuiElement { get; set; }
    }

    public class PlantRecommendationItemDto
    {
        public string EntityType { get; set; } = null!;
        public int EntityId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? ImageUrl { get; set; }
        public string? FengShuiElement { get; set; }
        public string? FengShuiMeaning { get; set; }
        public string? SuitableSpace { get; set; }
        public double MatchScore { get; set; }
        public string? ReasonForRecommendation { get; set; }
        public int NurseryId { get; set; }
        public string? NurseryName { get; set; }
    }

    public class PlantSuggestionResponseDto
    {
        public string EntityType { get; set; } = null!;
        public int EntityId { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? ImageUrl { get; set; }
        public bool IsPurchasable { get; set; }
        public double RelevanceScore { get; set; }
    }

    public class AIChatbotResponseDto
    {
        public string Intent { get; set; } = "general";
        public string Reply { get; set; } = string.Empty;
        public string? RoomEnvironmentSummary { get; set; }
        public List<PlantSuggestionResponseDto> SuggestedPlants { get; set; } = new();
        public List<string> CareTips { get; set; } = new();
        public List<string> FollowUpQuestions { get; set; } = new();
        public List<PolicyGroundingSourceDto> PolicySources { get; set; } = new();
        public string? Disclaimer { get; set; }
        public bool UsedFallback { get; set; }
    }

    public class PolicyGroundingSourceDto
    {
        public int PolicyContentId { get; set; }
        public int? Category { get; set; }
        public string? Title { get; set; }
        public string? Excerpt { get; set; }
    }

    public class AIChatSessionResponseDto
    {
        public int SessionId { get; set; }
        public string? Title { get; set; }
        public DateTime? StartedAt { get; set; }
        public int Status { get; set; }
    }
}
