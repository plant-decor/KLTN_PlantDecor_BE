using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserPreferencesController : ControllerBase
    {
        private readonly IUserPreferenceService _userPreferenceService;

        public UserPreferencesController(IUserPreferenceService userPreferenceService)
        {
            _userPreferenceService = userPreferenceService;
        }

        /// <summary>
        /// Kich hoat tinh toan lai toan bo diem preference (Admin/Test)
        /// </summary>
        [HttpPost("recalculate")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RecalculateAllUserPreferences()
        {
            await _userPreferenceService.CalculatedAllUserPreferenceAsync();

            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Recalculated all user preferences successfully"
            });
        }

        /// <summary>
        /// Kich hoat tinh toan lai diem preference cho 1 user (Admin/Test)
        /// </summary>
        [HttpPost("recalculate/{userId:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RecalculateUserPreferences(int userId)
        {
            await _userPreferenceService.CalculateUserPreferenceForUserAsync(userId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Recalculated user preferences successfully for userId={userId}"
            });
        }

        /// <summary>
        /// Lay danh sach cay goi y cho user hien tai
        /// </summary>
        [HttpGet("recommendations")]
        public async Task<IActionResult> GetRecommendations([FromQuery] int limit = 10)
        {
            var userId = GetUserId();
            var data = await _userPreferenceService.GetTopRecommendationsAsync(userId, limit);

            return Ok(new ApiResponse<List<UserPreferenceRecommendationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get recommendations successfully",
                Payload = data
            });
        }

        /// <summary>
        /// Lay danh sach cay goi y theo boi canh dang xem/tim kiem gan day
        /// </summary>
        [HttpGet("recommendations/contextual")]
        public async Task<IActionResult> GetContextualRecommendations([FromQuery] int limit = 10, [FromQuery] int? seedPlantId = null)
        {
            var userId = GetUserId();
            var data = await _userPreferenceService.GetContextualRecommendationsAsync(userId, limit, seedPlantId);

            return Ok(new ApiResponse<List<UserPreferenceRecommendationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get contextual recommendations successfully",
                Payload = data
            });
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }

            return userId;
        }
    }
}
