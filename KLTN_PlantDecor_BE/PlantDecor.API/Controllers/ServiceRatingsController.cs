using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API đánh giá dịch vụ chăm sóc
    /// </summary>
    [Route("api/service-ratings")]
    [ApiController]
    [Authorize]
    public class ServiceRatingsController : ControllerBase
    {
        private readonly IServiceRatingService _serviceRatingService;

        public ServiceRatingsController(IServiceRatingService serviceRatingService)
        {
            _serviceRatingService = serviceRatingService;
        }

        /// <summary>
        /// [Customer] Đánh giá dịch vụ sau khi hoàn thành
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateRating([FromBody] CreateServiceRatingRequestDto request)
        {
            var userId = GetUserId();
            var result = await _serviceRatingService.CreateRatingAsync(userId, request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<ServiceRatingResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Rating submitted successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Any] Lấy đánh giá theo ServiceRegistration
        /// </summary>
        [HttpGet("by-registration/{registrationId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetByRegistrationId(int registrationId)
        {
            var result = await _serviceRatingService.GetByRegistrationIdAsync(registrationId);

            return Ok(new ApiResponse<ServiceRatingResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get rating successfully",
                Payload = result
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
