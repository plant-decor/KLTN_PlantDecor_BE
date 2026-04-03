namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class RoomDesignResponseDto
    {
        /// <summary>
        /// AI analysis of the room
        /// </summary>
        public RoomAnalysisDto RoomAnalysis { get; set; } = null!;

        /// <summary>
        /// List of recommended plants
        /// </summary>
        public List<PlantRecommendationDto> Recommendations { get; set; } = new();

        /// <summary>
        /// Total number of recommendations
        /// </summary>
        public int TotalCount { get; set; }

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }
    }

    public class RoomAnalysisDto
    {
        /// <summary>
        /// Detected room type (living room, bedroom, office, etc.)
        /// </summary>
        public string RoomType { get; set; } = null!;

        /// <summary>
        /// Estimated room size (small, medium, large)
        /// </summary>
        public string RoomSize { get; set; } = null!;

        /// <summary>
        /// Lighting condition (low, medium, high, natural)
        /// </summary>
        public string LightingCondition { get; set; } = null!;

        /// <summary>
        /// Detected interior style (modern, minimalist, tropical, etc.)
        /// </summary>
        public string InteriorStyle { get; set; } = null!;

        /// <summary>
        /// Available space for plants
        /// </summary>
        public string AvailableSpace { get; set; } = null!;

        /// <summary>
        /// Color palette of the room
        /// </summary>
        public List<string> ColorPalette { get; set; } = new();

        /// <summary>
        /// AI suggestions for plant placement
        /// </summary>
        /// <summary>
        /// Overall recommendation summary
        /// </summary>
        public string Summary { get; set; } = null!;
    }

    public class PlantRecommendationDto
    {
        /// <summary>
        /// Entity type (CommonPlant, PlantInstance, NurseryPlantCombo)
        /// </summary>
        public string EntityType { get; set; } = null!;

        /// <summary>
        /// Entity ID in database
        /// </summary>
        public int EntityId { get; set; }

        /// <summary>
        /// Product ID for purchase
        /// </summary>
        public int? ProductId { get; set; }

        /// <summary>
        /// Plant name
        /// </summary>
        public string Name { get; set; } = null!;

        /// <summary>
        /// Plant description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Price
        /// </summary>
        public decimal? Price { get; set; }

        /// <summary>
        /// Primary image URL
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Feng shui element
        /// </summary>
        public string? FengShuiElement { get; set; }

        /// <summary>
        /// Match score (0-1)
        /// </summary>
        public double MatchScore { get; set; }

        /// <summary>
        /// Nursery ID
        /// </summary>
        public int NurseryId { get; set; }

        /// <summary>
        /// Nursery name
        /// </summary>
        public string? NurseryName { get; set; }

        /// <summary>
        /// AI-generated reason for recommendation
        /// </summary>
        public string ReasonForRecommendation { get; set; } = null!;

        /// <summary>
        /// Suggested placement in the room
        /// </summary>
        public string? SuggestedPlacement { get; set; }

        /// <summary>
        /// Care difficulty level
        /// </summary>
        public string? CareDifficulty { get; set; }

        /// <summary>
        /// Whether the plant is currently available for purchase
        /// </summary>
        public bool IsPurchasable { get; set; }
    }
}
