using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API về support conversation
    /// </summary>
    [Route("api/support-conversations")]
    [ApiController]
    [Authorize]
    public class SupportConversationsController : ControllerBase
    {
        private readonly IChatService _chatService;

        public SupportConversationsController(IChatService chatService)
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
        [HttpGet("{conversationId:int}")]
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
        [HttpGet("{conversationId:int}/messages")]
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

        [HttpPost("start")]
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> Start([FromBody] StartSupportConversationRequestDto request)
        {
            var userId = GetUserId();
            var conversation = await _chatService.StartSupportConversationAsync(userId, request.FirstMessage);

            return StatusCode(StatusCodes.Status201Created, new ApiResponse<ConversationResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Support conversation started successfully",
                Payload = conversation
            });
        }

        [HttpGet("waiting")]
        [Authorize(Roles = "Consultant")]
        public async Task<IActionResult> GetWaiting()
        {
            var conversations = await _chatService.GetWaitingSupportConversationsAsync();
            return Ok(new ApiResponse<List<ConversationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get waiting conversations successfully",
                Payload = conversations
            });
        }

        [HttpGet("my-claimed")]
        [Authorize(Roles = "Consultant")]
        public async Task<IActionResult> GetMyClaimed()
        {
            var userId = GetUserId();
            var conversations = await _chatService.GetMyClaimedSupportConversationsAsync(userId);

            return Ok(new ApiResponse<List<ConversationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get claimed conversations successfully",
                Payload = conversations
            });
        }

        [HttpPost("{conversationId:int}/claim")]
        [Authorize(Roles = "Consultant")]
        public async Task<IActionResult> Claim(int conversationId)
        {
            var userId = GetUserId();
            var success = await _chatService.ClaimSupportConversationAsync(userId, conversationId);

            if (!success)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status400BadRequest,
                    Message = "Conversation is no longer waiting",
                    Payload = null
                });
            }

            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Conversation claimed successfully",
                Payload = null
            });
        }

        [HttpPost("{conversationId:int}/close")]
        public async Task<IActionResult> Close(int conversationId)
        {
            var userId = GetUserId();
            await _chatService.CloseConversationAsync(userId, conversationId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Conversation closed successfully",
                Payload = null
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
