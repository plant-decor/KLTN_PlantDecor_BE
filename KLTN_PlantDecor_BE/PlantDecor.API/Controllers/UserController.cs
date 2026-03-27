using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API về thông tin người dùng
    /// </summary>
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

        [HttpGet("user-profile")]
        public async Task<IActionResult> GetUserProfile()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }
            var user = await _userService.GetByIdAsync(userId);
            if (user == null)
            {
                throw new NotFoundException("User not found");
            }
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "User profile retrieved successfully",
                Payload = user
            });
        }

        [HttpPut("user-profile")]
        public async Task<IActionResult> UpdateUserInfo([FromBody] UserUpdateDto request)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }
            var updatedUser = await _userService.UpdateUserInfoAsync(userId, request);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "User information updated successfully",
                Payload = updatedUser
            });
        }

        [HttpPut("user-email")]
        public async Task<IActionResult> UpdateUserEmail([FromBody] EmailUpdateDto emailUpdate)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }
            var result = await _userService.UpdateEmailAsync(userId, emailUpdate);
            if (!result)
            {
                throw new Exception("Failed to update email");
            }
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Email updated successfully",
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
        [HttpPost("set-password-for-google-login")]
        public async Task<IActionResult> SetPassword([FromBody] SetPasswordDto dto)
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }
            var result = await _userService.SetPasswordAsync(userId, dto);
            if (!result)
            {
                throw new Exception("Failed to set password");
            }
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Password set successfully"
            });
        }
    }
}

