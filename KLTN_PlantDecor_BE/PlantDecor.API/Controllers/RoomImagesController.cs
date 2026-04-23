using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Enums;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoomImagesController : ControllerBase
    {
        private readonly IRoomImageService _roomImageService;

        public RoomImagesController(IRoomImageService roomImageService)
        {
            _roomImageService = roomImageService;
        }

        [HttpPost("upload")]
        [Authorize(Roles = "Customer")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(UploadRoomImagesResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Upload([FromForm] UploadRoomImagesRequest request)
        {
            if (request.Images == null || request.Images.Count == 0)
            {
                throw new BadRequestException("At least one room image file is required");
            }

            var userId = GetRequiredUserId();
            var result = await _roomImageService.UploadRoomImagesAsync(request, userId);

            return Ok(new ApiResponse<UploadRoomImagesResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Uploaded {result.RoomImages.Count} room images successfully",
                Payload = result
            });
        }

        [HttpGet("GetAllRoomImagesByUserId")]
        [Authorize(Roles = "Customer")]
        [ProducesResponseType(typeof(UploadRoomImagesResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAllRoomImagesByUserId()
        {
            var userId = GetRequiredUserId();
            var result = await _roomImageService.GetAllRoomImagesByUserIdAsync(userId);

            return Ok(new ApiResponse<UploadRoomImagesResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved room images successfully",
                Payload = result
            });
        }

        [HttpGet("GetALLRoomImagesByUserIdAndViewAngle/{viewAngle}")]
        [Authorize(Roles = "Customer")]
        [ProducesResponseType(typeof(UploadRoomImagesResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetALLRoomImagesByUserIdAndViewAngle([FromRoute] RoomViewAngleEnum viewAngle)
        {
            var userId = GetRequiredUserId();
            var result = await _roomImageService.GetAllRoomImagesByUserIdAndViewAngleAsync(userId, viewAngle);

            return Ok(new ApiResponse<UploadRoomImagesResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Retrieved room images by view angle successfully",
                Payload = result
            });
        }

        private int GetRequiredUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }

            return userId;
        }
    }
}
