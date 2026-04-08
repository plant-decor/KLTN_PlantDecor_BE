using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IWishlistRepository : IGenericRepository<Wishlist>
    {
        Task<PaginatedResult<Wishlist>> GetByUserIdWithPaginationAsync(int userId, Pagination pagination);
        Task<Wishlist?> GetByUserAndItemAsync(int userId, WishlistItemType itemType, int itemId);
        Task<bool> ExistsAsync(int userId, WishlistItemType itemType, int itemId);
        Task<int> ClearByUserIdAsync(int userId);
        Task SoftDeletePlantInstanceWishlistsAsync(int plantInstanceId);
    }
}
