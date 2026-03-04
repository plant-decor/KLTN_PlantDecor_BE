using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Helpers;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API quản lý Vựa (Nursery) cho Manager
    /// </summary>
    [Route("api/manager/[controller]")]
    [ApiController]
    //[Authorize(Roles = "Manager")]
    public class NurseriesController : ControllerBase
    {
        private readonly INurseryService _nurseryService;

        public NurseriesController(INurseryService nurseryService)
        {
            _nurseryService = nurseryService;
        }

        #region Manager Operations

        /// <summary>
        /// Lấy thông tin vựa của Manager đang đăng nhập
        /// </summary>
        [HttpGet("my-nursery")]
        public async Task<IActionResult> GetMyNursery()
        {
            var managerId = GetCurrentUserId();
            var nursery = await _nurseryService.GetMyNurseryAsync(managerId);
            
            if (nursery == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = "Bạn chưa có vựa nào"
                });

            return Ok(new ApiResponse<NurseryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy thông tin vựa thành công",
                Payload = nursery
            });
        }

        /// <summary>
        /// Tạo vựa mới cho Manager
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateNursery([FromBody] NurseryRequestDto request)
        {
            var managerId = GetCurrentUserId();
            var nursery = await _nurseryService.CreateNurseryAsync(managerId, request);
            return CreatedAtAction(nameof(GetMyNursery), new ApiResponse<NurseryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Tạo vựa thành công",
                Payload = nursery
            });
        }

        /// <summary>
        /// Cập nhật thông tin vựa của Manager
        /// </summary>
        [HttpPut("my-nursery")]
        public async Task<IActionResult> UpdateMyNursery([FromBody] NurseryUpdateDto request)
        {
            var managerId = GetCurrentUserId();
            var nursery = await _nurseryService.UpdateMyNurseryAsync(managerId, request);
            return Ok(new ApiResponse<NurseryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật vựa thành công",
                Payload = nursery
            });
        }

        #endregion

        #region Admin Operations

        /// <summary>
        /// [Admin] Lấy tất cả vựa
        /// </summary>
        [HttpGet("/api/admin/nurseries")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAllNurseries([FromQuery] Pagination pagination)
        {
            var nurseries = await _nurseryService.GetAllNurseriesAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<NurseryListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách vựa thành công",
                Payload = nurseries
            });
        }

        /// <summary>
        /// [Admin] Lấy vựa theo ID
        /// </summary>
        [HttpGet("/api/admin/nurseries/{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetNurseryById(int id)
        {
            var nursery = await _nurseryService.GetNurseryByIdAsync(id);
            if (nursery == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = $"Vựa với ID {id} không tồn tại"
                });

            return Ok(new ApiResponse<NurseryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy thông tin vựa thành công",
                Payload = nursery
            });
        }

        /// <summary>
        /// [Admin] Cập nhật vựa theo ID
        /// </summary>
        [HttpPut("/api/admin/nurseries/{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateNursery(int id, [FromBody] NurseryUpdateDto request)
        {
            var nursery = await _nurseryService.UpdateNurseryAsync(id, request);
            return Ok(new ApiResponse<NurseryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Cập nhật vựa thành công",
                Payload = nursery
            });
        }

        /// <summary>
        /// [Admin] Xóa vựa (soft delete)
        /// </summary>
        [HttpDelete("/api/admin/nurseries/{id}")]
        //[Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteNursery(int id)
        {
            await _nurseryService.DeleteNurseryAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Xóa vựa thành công"
            });
        }

        #endregion

        #region Public Operations

        /// <summary>
        /// Lấy danh sách vựa đang hoạt động (công khai)
        /// </summary>
        [HttpGet("/api/nurseries")]
        [AllowAnonymous]
        public async Task<IActionResult> GetActiveNurseries([FromQuery] Pagination pagination)
        {
            var nurseries = await _nurseryService.GetActiveNurseriesAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<NurseryListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách vựa thành công",
                Payload = nurseries
            });
        }

        #endregion

        #region Private Methods

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                // For testing, return a default manager ID
                return 1;
            }
            return userId;
        }

        #endregion
    }
}
