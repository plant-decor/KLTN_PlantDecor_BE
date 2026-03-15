using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Helpers;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CartController : ControllerBase
    {
        private readonly ICartService _cartService;

        public CartController(ICartService cartService)
        {
            _cartService = cartService;
        }

        /// <summary>
        /// Lấy giỏ hàng của user hiện tại (có phân trang)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetCart([FromQuery] Pagination pagination)
        {
            var userId = GetUserId();
            var result = await _cartService.GetCartByUserIdAsync(userId, pagination);
            return Ok(new ApiResponse<PaginatedResult<CartItemResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get cart successfully",
                Payload = result
            });
        }

        /// <summary>
        /// Thêm sản phẩm vào giỏ hàng (tăng số lượng nếu đã tồn tại)
        /// </summary>
        [HttpPost("items")]
        public async Task<IActionResult> AddItem([FromBody] CartItemRequestDto request)
        {
            var userId = GetUserId();
            var item = await _cartService.AddItemAsync(userId, request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<CartItemResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Add item to cart successfully",
                Payload = item
            });
        }

        /// <summary>
        /// Cập nhật số lượng của một cart item
        /// </summary>
        [HttpPatch("items/{cartItemId}")]
        public async Task<IActionResult> UpdateItemQuantity(int cartItemId, [FromBody] UpdateCartItemDto request)
        {
            var userId = GetUserId();
            var item = await _cartService.UpdateItemQuantityAsync(userId, cartItemId, request);
            return Ok(new ApiResponse<CartItemResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Update cart item successfully",
                Payload = item
            });
        }

        /// <summary>
        /// Xóa một sản phẩm khỏi giỏ hàng
        /// </summary>
        [HttpDelete("items/{cartItemId}")]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            var userId = GetUserId();
            await _cartService.RemoveItemAsync(userId, cartItemId);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Remove item from cart successfully"
            });
        }

        /// <summary>
        /// Xóa toàn bộ sản phẩm trong giỏ hàng
        /// </summary>
        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            var userId = GetUserId();
            await _cartService.ClearCartAsync(userId);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Clear cart successfully"
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
