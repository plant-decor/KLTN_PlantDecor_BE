using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IWishlistService
    {
        Task<PaginatedResult<WishlistItemResponseDto>> GetWishlistByUserIdAsync(int userId, Pagination pagination);
        Task<WishlistItemResponseDto> AddToWishlistAsync(int userId, int plantId);
        Task<bool> RemoveFromWishlistAsync(int userId, int plantId);
        Task<bool> IsInWishlistAsync(int userId, int plantId);
    }
}
