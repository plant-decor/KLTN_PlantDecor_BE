using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IWishlistService
    {
        Task<PaginatedResult<WishlistItemResponseDto>> GetWishlistByUserIdAsync(int userId, Pagination pagination);
        Task<WishlistItemResponseDto> AddToWishlistAsync(int userId, WishlistItemType itemType, int itemId);
        Task<bool> RemoveFromWishlistAsync(int userId, WishlistItemType itemType, int itemId);
        Task<bool> IsInWishlistAsync(int userId, WishlistItemType itemType, int itemId);
    }
}
