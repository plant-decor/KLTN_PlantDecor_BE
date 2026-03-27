namespace PlantDecor.BusinessLogicLayer.DTOs.Requests
{
    public class CreateConversationRequestDto
    {
        public int OtherUserId { get; set; }
    }

    public class SendMessageRequestDto
    {
        public string Content { get; set; } = string.Empty;
    }
}
