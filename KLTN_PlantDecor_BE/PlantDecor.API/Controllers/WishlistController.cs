using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Helpers;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class WishlistController : ControllerBase
    {
        private readonly IWishlistService _wishlistService;

        public WishlistController(IWishlistService wishlistService)
        {
            _wishlistService = wishlistService;
        }

        /// <summary>
        /// Lấy danh sách wishlist của user hiện tại (có phân trang)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetWishlist([FromQuery] Pagination pagination)
        {
            var userId = GetUserId();
            var result = await _wishlistService.GetWishlistByUserIdAsync(userId, pagination);
            return Ok(new ApiResponse<PaginatedResult<WishlistItemResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get wishlist successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Thêm cây vào wishlist
        /// </summary>
        [HttpPost("{plantId}")]
        public async Task<IActionResult> AddToWishlist(int plantId)
        {
            var userId = GetUserId();
            var item = await _wishlistService.AddToWishlistAsync(userId, plantId);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<WishlistItemResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Add to wishlist successfully",
                Payload = item
            });
        }

        /// <summary>
        /// Xóa cây khỏi wishlist
        /// </summary>
        [HttpDelete("{plantId}")]
        public async Task<IActionResult> RemoveFromWishlist(int plantId)
        {
            var userId = GetUserId();
            await _wishlistService.RemoveFromWishlistAsync(userId, plantId);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Remove from wishlist successfully"
            });
        }

        /// <summary>
        /// Kiểm tra cây có trong wishlist không
        /// </summary>
        [HttpGet("{plantId}/check")]
        public async Task<IActionResult> IsInWishlist(int plantId)
        {
            var userId = GetUserId();
            var exists = await _wishlistService.IsInWishlistAsync(userId, plantId);

            if (exists)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Plant is in wishlist",
                    Payload = true
                });
            }
            else
            {
                throw new NotFoundException("Plant is not in wishlist");
            }
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
