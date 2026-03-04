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
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthenticationService _authenticationService;

        public UserController(IUserService userService, IAuthenticationService authenticationService)
        {
            _userService = userService;
            _authenticationService = authenticationService;
        }

        [HttpPost("create-manager")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateManagerAccount([FromBody] CreateManagerRequest request)
        {
            if (!ModelState.IsValid)
            {
                throw new BadRequestException("Invalid request");
            }

            var result = await _authenticationService.CreateManagerAsync(request);

            if (result == null)
            {
                throw new Exception("Failed to create manager account");
            }

            return StatusCode(StatusCodes.Status201Created, new ApiResponse<AuthenticationResponse>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Manager account created successfully!",
                Payload = result
            });
        }

        //   [Authorize(Roles = "Admin,User")]
        [HttpPut("avatar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateAvatar(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new BadRequestException("No file was uploaded");
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }

            // Upload the file to Cloudinary
            var uploadResult = await _userService.UpdateAvatar(userId, file);
            if (!uploadResult)
            {
                throw new Exception("Failed to upload avatar");
            }

            var updatedUser = await _userService.GetByIdAsync(userId);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Avatar uploaded successfully",
                Payload = new
                {
                    avatarURL = updatedUser.AvatarUrl
                }
            });
        }
    }
}

