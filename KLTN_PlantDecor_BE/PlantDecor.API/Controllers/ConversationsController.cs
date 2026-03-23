using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ConversationsController : ControllerBase
    {
        private readonly IChatService _chatService;

        public ConversationsController(IChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Lấy danh sách tất cả conversations của user hiện tại
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyConversations()
        {
            var userId = GetUserId();
            var conversations = await _chatService.GetUserConversationsAsync(userId);
            return Ok(new ApiResponse<List<ConversationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get conversations successfully",
                Payload = conversations
            });
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một conversation
        /// </summary>
        [HttpGet("{conversationId}")]
        public async Task<IActionResult> GetConversationDetails(int conversationId)
        {
            var userId = GetUserId();
            var conversation = await _chatService.GetConversationDetailsAsync(userId, conversationId);
            return Ok(new ApiResponse<ConversationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get conversation details successfully",
                Payload = conversation
            });
        }

        /// <summary>
        /// Lấy lịch sử tin nhắn của một conversation (có phân trang)
        /// </summary>
        [HttpGet("{conversationId}/messages")]
        public async Task<IActionResult> GetConversationMessages(
            int conversationId,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            var userId = GetUserId();
            var messages = await _chatService.GetConversationMessagesAsync(userId, conversationId, pageNumber, pageSize);
            return Ok(new ApiResponse<ConversationMessagesResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get messages successfully",
                Payload = messages
            });
        }

        /// <summary>
        /// Tạo conversation mới với một user khác
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateConversation([FromBody] CreateConversationRequestDto request)
        {
            var userId = GetUserId();
            var conversation = await _chatService.CreateConversationAsync(userId, request.OtherUserId);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<ConversationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Conversation created successfully",
                Payload = conversation
            });
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedAccessException("Unable to identify user from token");
            }
            return userId;
        }
    }
}
