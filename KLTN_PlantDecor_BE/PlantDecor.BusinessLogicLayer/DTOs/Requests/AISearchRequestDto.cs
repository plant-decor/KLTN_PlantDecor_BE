namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class SemanticSearchRequestDto
    {
        public string Query { get; set; } = null!;
        public List<string>? EntityTypes { get; set; }
        public int? Limit { get; set; }
        public bool OnlyPurchasable { get; set; } = true;
    }

    public class RoomRecommendationRequestDto
    {
        public string RoomDescription { get; set; } = null!;
        public string? FengShuiElement { get; set; }
        public decimal? MaxBudget { get; set; }
        public int? Limit { get; set; }
        public List<string>? PreferredRooms { get; set; }
        public bool? PetSafe { get; set; }
        public bool? ChildSafe { get; set; }
    }

    public class PlantSuggestionQueryDto
    {
        public string? Description { get; set; }
        public string? FengShuiElement { get; set; }
        public string? RoomType { get; set; }
        public bool? OnlyPurchasable { get; set; }
        public int? Limit { get; set; }
        public decimal? MaxBudget { get; set; }
    }
}
