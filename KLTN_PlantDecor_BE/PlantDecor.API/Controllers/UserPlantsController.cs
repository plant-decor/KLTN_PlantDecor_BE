using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Helpers;
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
        private readonly ICareReminderService _careReminderService;

        public UserPlantsController(IUserPlantService userPlantService, ICareReminderService careReminderService)
        {
            _userPlantService = userPlantService;
            _careReminderService = careReminderService;
        }

        /// <summary>
        /// [Customer] Lay danh sach cay cua user hien tai
        /// </summary>
        [HttpGet("my")]
        public async Task<IActionResult> GetMyPlant()
        {
            var userId = GetUserId();
            var userPlants = await _userPlantService.GetMyPlantsAsync(userId);
            if (userPlants == null || userPlants.Count == 0)
            {
                return Ok(new ApiResponse<List<UserPlantResponseDto>>
                {
                    Success = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "You don't have any plants yet",
                    Payload = new List<UserPlantResponseDto>()
                });
            }

            return Ok(new ApiResponse<List<UserPlantResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get my plants successfully",
                Payload = userPlants
            });
        }

        /// <summary>
        /// [Customer] Lay danh sach thong bao nhac cham soc cay
        /// </summary>
        [HttpGet("my-care-reminders")]
        public async Task<IActionResult> GetMyCareReminders([FromQuery] int? careType, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            var userId = GetUserId();
            var pagination = new Pagination(pageNumber, pageSize);
            var reminders = await _userPlantService.GetMyCareRemindersAsync(userId, careType, pagination);
            if (reminders.Items == null || !reminders.Items.Any())
            {
                return Ok(new ApiResponse<PaginatedResult<CareReminderNotificationResponseDto>>
                {
                    Success = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "You don't have any care reminders yet",
                    Payload = new PaginatedResult<CareReminderNotificationResponseDto>(
                        new List<CareReminderNotificationResponseDto>(),
                        0,
                        pagination.PageNumber,
                        pagination.PageSize)
                });
            }

            return Ok(new ApiResponse<PaginatedResult<CareReminderNotificationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get care reminders successfully",
                Payload = reminders
            });
        }

        /// <summary>
        /// [Customer] Lay danh sach thong bao nhac cham soc cay hom nay
        /// </summary>
        [HttpGet("my-care-reminders/today")]
        public async Task<IActionResult> GetMyCareRemindersToday()
        {
            var userId = GetUserId();
            var reminders = await _userPlantService.GetMyCareRemindersTodayAsync(userId);
            if (reminders == null || reminders.Count == 0)
            {
                return Ok(new ApiResponse<List<CareReminderNotificationResponseDto>>
                {
                    Success = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "You don't have any care reminders today",
                    Payload = new List<CareReminderNotificationResponseDto>()
                });
            }

            return Ok(new ApiResponse<List<CareReminderNotificationResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get today's care reminders successfully",
                Payload = reminders
            });
        }

        /// <summary>
        /// [Customer] Tạo care reminder cho cây của tôi
        /// </summary>
        [HttpPost("my-care-reminders")]
        public async Task<IActionResult> CreateMyCareReminder([FromBody] CreateCareReminderRequestDto request)
        {
            var userId = GetUserId();
            var result = await _careReminderService.CreateForUserAsync(userId, request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<CareReminderResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Create care reminder successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Customer] Cập nhật care reminder của tôi
        /// </summary>
        [HttpPut("my-care-reminders/{id:int}")]
        public async Task<IActionResult> UpdateMyCareReminder(int id, [FromBody] UpdateCareReminderRequestDto request)
        {
            var userId = GetUserId();
            var result = await _careReminderService.UpdateForUserAsync(userId, id, request);
            return Ok(new ApiResponse<CareReminderResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Update care reminder successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Customer] Đánh dấu care reminder của tôi đã hoàn thành
        /// </summary>
        [HttpPatch("my-care-reminders/{id:int}/complete")]
        public async Task<IActionResult> CompleteMyCareReminder(int id)
        {
            var userId = GetUserId();
            var result = await _careReminderService.CompleteForUserAsync(userId, id);
            return Ok(new ApiResponse<CareReminderResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Complete care reminder successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Customer] Xóa care reminder của tôi
        /// </summary>
        [HttpDelete("my-care-reminders/{id:int}")]
        public async Task<IActionResult> DeleteMyCareReminder(int id)
        {
            var userId = GetUserId();
            await _careReminderService.DeleteForUserAsync(userId, id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Delete care reminder successfully",
                Payload = null
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
