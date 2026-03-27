using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Enums;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class TrackingController : ControllerBase
    {
        private readonly IUserBehaviorLogService _userBehaviorLogService;

        public TrackingController(IUserBehaviorLogService userBehaviorLogService)
        {
            _userBehaviorLogService = userBehaviorLogService;
        }

        [HttpPost("log-action")]
        public async Task<IActionResult> LogAction([FromQuery] int plantId, [FromQuery] UserActionTypeEnum actionType)
        {
            if (plantId <= 0)
            {
                throw new BadRequestException("PlantId must be greater than 0");
            }

            var userId = GetUserId();
            await _userBehaviorLogService.LogUserActionAsync(userId, plantId, actionType);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Track user action successfully"
            });
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                throw new UnauthorizedException("Unable to identify user from token");
            return userId;
        }
    }
}
