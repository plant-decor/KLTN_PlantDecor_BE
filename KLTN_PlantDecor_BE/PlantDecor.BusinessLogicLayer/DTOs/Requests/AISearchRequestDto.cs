using PlantDecor.DataAccessLayer.Enums;

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

    public class AIChatbotRequestDto
    {
        /// <summary>
        /// Optional. If omitted or <= 0, server will reuse latest active session or auto-create one.
        /// </summary>
        public int SessionId { get; set; }

        /// <summary>
        /// Optional. If provided (Customer only), chatbot can recommend care service packages based on plants in this order.
        /// </summary>
        public int? OrderId { get; set; }

        /// <summary>
        /// Required user message for server-side intent analysis.
        /// </summary>
        public string Message { get; set; } = null!;
        public string? RoomDescription { get; set; }
        public FengShuiElementTypeEnum? FengShuiElement { get; set; }
        public decimal? MaxBudget { get; set; }
        public int? Limit { get; set; }
        public List<RoomTypeEnum>? PreferredRooms { get; set; }
        public bool? PetSafe { get; set; }
        public bool? ChildSafe { get; set; }
        public bool OnlyPurchasable { get; set; } = true;

        /// <summary>
        /// Deprecated in server-managed session mode. Kept for backward compatibility.
        /// </summary>
        public List<ChatbotConversationTurnDto>? ConversationHistory { get; set; }
    }

    public class AIChatSessionCreateRequestDto
    {
        public string? Title { get; set; }
    }

    public class AIChatSessionRenameRequestDto
    {
        public string? Title { get; set; }
    }

    public class ChatbotConversationTurnDto
    {
        /// <summary>
        /// Allowed values: user, assistant
        /// </summary>
        public string Role { get; set; } = "user";
        public string Content { get; set; } = string.Empty;
    }
}
