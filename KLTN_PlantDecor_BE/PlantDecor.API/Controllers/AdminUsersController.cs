using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API quan ly nguoi dung cho Admin
    /// </summary>
    [Route("api/admin/users")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAIQuotaService _aiQuotaService;
        private readonly IUserSubscriptionService _userSubscriptionService;

        public AdminUsersController(IUserService userService, IAIQuotaService aiQuotaService, IUserSubscriptionService userSubscriptionService)
        {
            _userService = userService;
            _aiQuotaService = aiQuotaService;
            _userSubscriptionService = userSubscriptionService;
        }

        /// <summary>
        /// Tim kiem danh sach nguoi dung (phan trang + filter)
        /// </summary>
        [HttpPost("search")]
        public async Task<IActionResult> SearchUsers([FromBody] UserSearchRequestDto request)
        {
            var result = await _userService.SearchUsersAsync(request ?? new UserSearchRequestDto());

            return Ok(new ApiResponse<PaginatedResult<UserResponse>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Search users successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Lay chi tiet nguoi dung theo ID
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                throw new NotFoundException("User not found");

            return Ok(new ApiResponse<UserResponse>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get user detail successfully",
                Payload = user
            });
        }

        /// <summary>
        /// Toggle trang thai active/deactive cua nguoi dung
        /// </summary>
        [HttpPatch("{id:int}/toggle-active")]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var existingUser = await _userService.GetByIdAsync(id);
            if (existingUser == null)
                throw new NotFoundException("User not found");

            if (existingUser.Status == UserStatusEnum.Active)
            {
                await _userService.Deactive(id);
            }
            else
            {
                await _userService.SetActive(id);
            }

            var updatedUser = await _userService.GetByIdAsync(id);

            return Ok(new ApiResponse<UserResponse>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = updatedUser?.Status == UserStatusEnum.Active
                    ? "User has been activated"
                    : "User has been deactivated",
                Payload = updatedUser
            });
        }

        [HttpGet("{id}/ai-quota")]
        public async Task<IActionResult> GetUserAIQuota(int id)
        {
            var result = await _aiQuotaService.GetUserQuotaStatusAsync(id);
            return Ok(new ApiResponse<UserQuotaStatusDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "User AI quota retrieved successfully",
                Payload = result
            });
        }

        [HttpGet("{id}/subscriptions")]
        public async Task<IActionResult> GetUserSubscriptions(int id)
        {
            var result = await _userSubscriptionService.GetByUserIdAsync(id);
            return Ok(new ApiResponse<List<UserSubscriptionResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "User subscriptions retrieved successfully",
                Payload = result
            });
        }
    }
}
