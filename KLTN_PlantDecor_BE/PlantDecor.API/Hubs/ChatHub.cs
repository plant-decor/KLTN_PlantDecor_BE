using Microsoft.AspNetCore.SignalR;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PlantDecor.API.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;

        public ChatHub(IChatService chatService)
        {
            _chatService = chatService;
        }

        private static string GroupName(int conversationId) => $"conversation:{conversationId}";

        public async Task SendMessage(int conversationId, string content)
        {
            var userId = GetUserId();

            if (!await _chatService.IsParticipantAsync(userId, conversationId))
                throw new HubException("Not a participant");

            var message = await _chatService.SendMessageAsync(userId, conversationId, content);

            await Clients.Group(GroupName(conversationId))
                .SendAsync("messageReceived", new
                {
                    messageId = message.Id,
                    conversationId,
                    senderId = userId,
                    content = message.Content,
                    sendAt = message.CreatedAt
                });
        }

        public async Task JoinConversation(int conversationId)
        {
            var userId = GetUserId();
            // Check if user is participant of the conversation
            var isParticipant = await _chatService.IsParticipantAsync(userId, conversationId);
            if (!isParticipant)
                throw new HubException("Not a participant of this conversation");
            // Add connection to the conversation group
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));
        }

        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(conversationId));
        }

        public async Task UserTyping(int conversationId)
        {
            var userId = GetUserId();

            if (!await _chatService.IsParticipantAsync(userId, conversationId))
                throw new HubException("Not a participant");

            await Clients.OthersInGroup(GroupName(conversationId))
                .SendAsync("userTyping", new
                {
                    conversationId,
                    userId
                });
        }

        public async Task UserStoppedTyping(int conversationId)
        {
            var userId = GetUserId();

            if (!await _chatService.IsParticipantAsync(userId, conversationId))
                throw new HubException("Not a participant");

            await Clients.OthersInGroup(GroupName(conversationId))
                .SendAsync("userStoppedTyping", new
                {
                    conversationId,
                    userId
                });
        }

        private int GetUserId()
        {
            var userIdClaim =
        Context.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Context.User.FindFirstValue(JwtRegisteredClaimNames.Sub); ;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedException("Unable to identify user from token");
            return userId;
        }
    }
}
