using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;

namespace PlantDecor.BusinessLogicLayer.Mappings
{
    public static class AIChatMapper
    {
        public static AIChatSessionListItemResponseDto ToSessionListItemResponse(this AIChatSession session)
        {
            if (session == null) return null!;

            return new AIChatSessionListItemResponseDto
            {
                SessionId = session.Id,
                Title = session.Title,
                Status = MapSessionStatus(session.Status),
                StartedAt = session.StartedAt,
                EndedAt = session.EndedAt,
                UpdatedAt = session.UpdatedAt
            };
        }

        public static List<AIChatSessionListItemResponseDto> ToSessionListItemResponses(this IEnumerable<AIChatSession> sessions)
        {
            return sessions.Select(ToSessionListItemResponse).ToList();
        }

        public static AIChatMessageHistoryItemResponseDto ToMessageHistoryItemResponse(this AIChatMessage message)
        {
            if (message == null) return null!;

            return new AIChatMessageHistoryItemResponseDto
            {
                MessageId = message.Id,
                Role = MapMessageRole(message.Role),
                Content = message.Content,
                Intent = message.Intent,
                IsFallback = message.IsFallback ?? false,
                IsPolicyResponse = message.IsPolicyResponse ?? false,
                CreatedAt = message.CreatedAt
            };
        }

        public static List<AIChatMessageHistoryItemResponseDto> ToMessageHistoryItemResponses(this IEnumerable<AIChatMessage> messages)
        {
            return messages.Select(ToMessageHistoryItemResponse).ToList();
        }

        private static string MapSessionStatus(int? status)
        {
            return status == (int)AIChatSessionStatusEnum.Closed ? "closed" : "active";
        }

        private static string MapMessageRole(int? role)
        {
            return role switch
            {
                (int)AIChatMessageRoleEnum.Assistant => "assistant",
                (int)AIChatMessageRoleEnum.System => "system",
                _ => "user"
            };
        }
    }
}
