using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API quản lý quota AI cho toàn hệ thống (Admin)
    /// </summary>
    [Route("api/admin/ai-quota")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class AdminAIQuotaController : ControllerBase
    {
        private readonly IMonthlyQuotaResetService _monthlyQuotaResetService;

        public AdminAIQuotaController(IMonthlyQuotaResetService monthlyQuotaResetService)
        {
            _monthlyQuotaResetService = monthlyQuotaResetService;
        }

        /// <summary>
        /// Kích hoạt reset quota miễn phí hàng tháng thủ công cho toàn bộ customer.
        /// Dùng để fix data hoặc test — Hangfire tự chạy vào ngày 1 mỗi tháng.
        /// </summary>
        [HttpPost("monthly-reset/trigger")]
        public async Task<IActionResult> TriggerMonthlyReset()
        {
            await _monthlyQuotaResetService.ResetMonthlyQuotaAsync();
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Monthly quota reset triggered successfully",
                Payload = null
            });
        }

        /// <summary>
        /// Reset quota miễn phí cho 1 user cụ thể (khi cần fix data thủ công).
        /// </summary>
        [HttpPost("monthly-reset/trigger/{userId}")]
        public async Task<IActionResult> TriggerMonthlyResetForUser(int userId)
        {
            await _monthlyQuotaResetService.GrantMonthlyFreeQuotaAsync(userId);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = $"Monthly quota reset triggered for user {userId}",
                Payload = null
            });
        }
    }
}
