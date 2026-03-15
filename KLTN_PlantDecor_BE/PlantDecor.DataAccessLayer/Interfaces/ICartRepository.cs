using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ICartRepository : IGenericRepository<Cart>
    {
        Task<Cart?> GetByUserIdAsync(int userId);
        Task<PaginatedResult<CartItem>> GetItemsByCartIdWithPaginationAsync(int cartId, Pagination pagination);
        Task<CartItem?> GetCartItemByIdAsync(int cartItemId);
        Task<bool> RemoveCartItemAsync(CartItem item);
        Task<int> ClearCartItemsAsync(int cartId);
    }
}
