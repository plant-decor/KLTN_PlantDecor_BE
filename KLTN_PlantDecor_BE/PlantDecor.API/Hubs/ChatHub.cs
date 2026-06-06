using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace PlantDecor.API.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IChatService _chatService;
        private readonly IOnlineTracker _onlineTracker;

        public ChatHub(IChatService chatService, IOnlineTracker onlineTracker)
        {
            _chatService = chatService;
            _onlineTracker = onlineTracker;
        }

        private static string GroupName(int conversationId) => $"conversation:{conversationId}";
        private static string UserGroupName(int userId) => $"user:{userId}";

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroupName(userId));

            bool isFirstConnection = !_onlineTracker.IsOnline(userId);
            _onlineTracker.UserConnected(userId, Context.ConnectionId);

            if (isFirstConnection && await _chatService.IsConsultantAsync(userId))
            {
                var conversationIds = await _chatService.GetActiveConversationIdsForConsultantAsync(userId);
                foreach (var conversationId in conversationIds)
                {
                    await Clients.Group(GroupName(conversationId))
                        .SendAsync("consultantStatusChanged", new { consultantId = userId, isOnline = true });
                }
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            bool isNowOffline = _onlineTracker.UserDisconnected(userId, Context.ConnectionId);

            if (isNowOffline && await _chatService.IsConsultantAsync(userId))
            {
                var conversationIds = await _chatService.GetActiveConversationIdsForConsultantAsync(userId);
                foreach (var conversationId in conversationIds)
                {
                    await Clients.Group(GroupName(conversationId))
                        .SendAsync("consultantStatusChanged", new { consultantId = userId, isOnline = false });
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

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

            var recipientIds = (await _chatService.GetParticipantIdsAsync(conversationId))
                .Where(participantId => participantId != userId)
                .Distinct()
                .ToList();

            foreach (var recipientId in recipientIds)
            {
                await Clients.Group(UserGroupName(recipientId))
                    .SendAsync("newMessageNotification", new
                    {
                        messageId = message.Id,
                        conversationId,
                        senderId = userId,
                        content = message.Content,
                        sendAt = message.CreatedAt
                    });
            }
        }

        public async Task JoinConversation(int conversationId)
        {
            var userId = GetUserId();
            var isParticipant = await _chatService.IsParticipantAsync(userId, conversationId);
            if (!isParticipant)
                throw new HubException("Not a participant of this conversation");
            await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));

            var participantIds = await _chatService.GetParticipantIdsAsync(conversationId);
            foreach (var participantId in participantIds)
            {
                if (participantId == userId) continue;
                if (!await _chatService.IsConsultantAsync(participantId)) continue;
                await Clients.Caller.SendAsync("consultantStatusChanged", new
                {
                    consultantId = participantId,
                    isOnline = _onlineTracker.IsOnline(participantId)
                });
                break;
            }
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
        Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? Context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedException("Unable to identify user from token");
            return userId;
        }
    }
}
