namespace PlantDecor.BusinessLogicLayer.DTOs.Responses
{
    public class ConversationResponseDto
    {
        public int Id { get; set; }
        public int? Status { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public List<ParticipantResponseDto> Participants { get; set; } = new();
        public MessageResponseDto? LatestMessage { get; set; }
        public List<MessageResponseDto>? Messages { get; set; }
        public int? TotalMessages { get; set; }
        public int? TotalPages { get; set; }
        public int? PageNumber { get; set; }
        public int? PageSize { get; set; }
    }

    public class ParticipantResponseDto
    {
        public int UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }
        public DateTime? JoinedAt { get; set; }
    }

    public class MessageResponseDto
    {
        public int Id { get; set; }
        public int? ChatSessionId { get; set; }
        public int? SenderId { get; set; }
        public string? SenderName { get; set; }
        public string? Content { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class ConversationMessagesResponseDto
    {
        public int ConversationId { get; set; }
        public List<MessageResponseDto> Messages { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
