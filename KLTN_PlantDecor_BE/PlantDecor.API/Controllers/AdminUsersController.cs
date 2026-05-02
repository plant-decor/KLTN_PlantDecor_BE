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

        public AdminUsersController(IUserService userService)
        {
            _userService = userService;
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
    }
}
