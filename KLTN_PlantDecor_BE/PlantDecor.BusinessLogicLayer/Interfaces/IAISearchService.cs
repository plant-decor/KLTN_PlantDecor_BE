using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IAISearchService
    {
        /// <summary>
        /// Semantic search for products with purchasable filter
        /// </summary>
        Task<SemanticSearchResponseDto> SearchPurchasableAsync(
            string query,
            List<string>? entityTypes = null,
            int limit = 10,
            bool onlyPurchasable = true);

        /// <summary>
        /// Get plant recommendations for room design based on description and preferences
        /// </summary>
        Task<RoomRecommendationResponseDto> GetRoomRecommendationsAsync(
            string roomDescription,
            string? fengShuiElement = null,
            decimal? maxBudget = null,
            int limit = 5,
            List<string>? preferredRooms = null,
            bool? petSafe = null,
            bool? childSafe = null);

        /// <summary>
        /// Suggest plants based on criteria
        /// </summary>
        Task<List<PlantSuggestionResponseDto>> SuggestPlantsAsync(
            string? description = null,
            string? fengShuiElement = null,
            string? roomType = null,
            bool onlyPurchasable = true,
            int limit = 10,
            decimal? maxBudget = null);

        /// <summary>
        /// Check if an entity is purchasable
        /// </summary>
        Task<bool> CheckPurchasableAsync(string entityType, int entityId);

        /// <summary>
        /// Create a new AI chat session for authenticated user
        /// </summary>
        Task<AIChatSessionResponseDto> CreateChatSessionAsync(int userId, string? title = null);

        /// <summary>
        /// AI chatbot for plant selection, room understanding and care consultation
        /// </summary>
        Task<AIChatbotResponseDto> ChatbotAsync(AIChatbotRequestDto request, int userId);
    }
}
