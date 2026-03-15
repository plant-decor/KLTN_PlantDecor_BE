using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ICartService
    {
        Task<PaginatedResult<CartItemResponseDto>> GetCartByUserIdAsync(int userId, Pagination pagination);
        Task<CartItemResponseDto> AddItemAsync(int userId, CartItemRequestDto request);
        Task<CartItemResponseDto> UpdateItemQuantityAsync(int userId, int cartItemId, UpdateCartItemDto request);
        Task<bool> RemoveItemAsync(int userId, int cartItemId);
        Task<bool> ClearCartAsync(int userId);
    }
}
