namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class RecommendByConversationRequestDto
    {
        public int ConversationId { get; set; }
        public int? MaxCandidates { get; set; }
    }
}
