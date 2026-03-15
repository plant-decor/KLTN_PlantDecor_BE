using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IWishlistRepository : IGenericRepository<Wishlist>
    {
        Task<PaginatedResult<Wishlist>> GetByUserIdWithPaginationAsync(int userId, Pagination pagination);
        Task<Wishlist?> GetByUserAndPlantAsync(int userId, int plantId);
        Task<bool> ExistsAsync(int userId, int plantId);
    }
}
