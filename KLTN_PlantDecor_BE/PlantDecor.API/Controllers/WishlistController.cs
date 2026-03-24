using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Enums;
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
        /// Thêm item vào wishlist (hỗ trợ nhiều loại: CommonPlant, PlantInstance, NurseryPlantCombo, NurseryMaterial)
        /// </summary>
        /// <param name="itemType">Loại item (0: CommonPlant, 1: PlantInstance, 2: NurseryPlantCombo, 3: NurseryMaterial)</param>
        /// <param name="itemId">ID của item</param>
        [HttpPost("{itemType}/{itemId}")]
        public async Task<IActionResult> AddToWishlist(WishlistItemType itemType, int itemId)
        {
            var userId = GetUserId();
            var item = await _wishlistService.AddToWishlistAsync(userId, itemType, itemId);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<WishlistItemResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Add to wishlist successfully",
                Payload = item
            });
        }

        /// <summary>
        /// Xóa item khỏi wishlist
        /// </summary>
        /// <param name="itemType">Loại item (0: CommonPlant, 1: PlantInstance, 2: NurseryPlantCombo, 3: NurseryMaterial)</param>
        /// <param name="itemId">ID của item</param>
        [HttpDelete("{itemType}/{itemId}")]
        public async Task<IActionResult> RemoveFromWishlist(WishlistItemType itemType, int itemId)
        {
            var userId = GetUserId();
            await _wishlistService.RemoveFromWishlistAsync(userId, itemType, itemId);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Remove from wishlist successfully"
            });
        }

        /// <summary>
        /// Kiểm tra item có trong wishlist không
        /// </summary>
        /// <param name="itemType">Loại item (0: CommonPlant, 1: PlantInstance, 2: NurseryPlantCombo, 3: NurseryMaterial)</param>
        /// <param name="itemId">ID của item</param>
        [HttpGet("{itemType}/{itemId}/check")]
        public async Task<IActionResult> IsInWishlist(WishlistItemType itemType, int itemId)
        {
            var userId = GetUserId();
            var exists = await _wishlistService.IsInWishlistAsync(userId, itemType, itemId);

            if (exists)
            {
                return Ok(new ApiResponse<bool>
                {
                    Success = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Item is in wishlist",
                    Payload = true
                });
            }
            else
            {
                throw new NotFoundException("Item is not in wishlist");
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
