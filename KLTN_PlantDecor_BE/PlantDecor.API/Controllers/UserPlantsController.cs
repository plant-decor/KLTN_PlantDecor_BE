using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    [Route("api/user-plants")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class UserPlantsController : ControllerBase
    {
        private readonly IUserPlantService _userPlantService;

        public UserPlantsController(IUserPlantService userPlantService)
        {
            _userPlantService = userPlantService;
        }

        /// <summary>
        /// [Customer] Lay danh sach cay cua user hien tai
        /// </summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyPlant()
        {
            var userId = GetUserId();
            var userPlants = await _userPlantService.GetMyPlantsAsync(userId);

            return Ok(new ApiResponse<List<UserPlantResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get my plants successfully",
                Payload = userPlants
            });
        }

        private int GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }

            return userId;
        }
    }
}