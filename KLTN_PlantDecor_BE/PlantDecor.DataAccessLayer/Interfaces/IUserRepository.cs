using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task<PaginatedResult<User>> GetAllAsyncWithPagination(Pagination pagination);
        Task<PaginatedResult<User>> SearchAsync(UserSearchFilter filter, Pagination pagination);
        Task SetActive(User user);
        Task DeActive(User user);
        Task<User?> GetByEmailAsync(string email);
        Task<List<User>> GetShippersByNurseryIdAsync(int nurseryId);
        Task<User?> GetByRefreshTokenAsync(string refreshToken);
        Task<bool> VerifyPasswordAsync(User user, string password);
        Task<List<RefreshToken>> GetRefreshTokenAsync(int userId);
        Task<User?> GetByPhoneAsync(string phone);
        Task<bool> IsVerifiedAsync(int userId);

        Task<bool> IsEmailExistsForOtherUserAsync(string email, int currentUserId);
        Task<List<RefreshToken>?> GetOldRefreshTokenByDeviceIdAsync(int userId, string deviceId);
        Task<RefreshToken?> GetRefreshTokenByDeviceIdAsync(int userId, string deviceId);
        Task<RefreshToken?> GetRefreshTokenByRefreshTokenAsync(int userId, string refreshToken);
        Task<int> DeleteRevokedRefreshTokensAsync();
        Task<List<User>> GetCaretakersByNurseryIdAsync(int nurseryId);
        Task<User?> GetCaretakerByIdWithSpecializationsAsync(int userId, int nurseryId);
        Task<List<User>> GetStaffAndCaretakersByNurseryIdAsync(int nurseryId);
        Task<User?> GetStaffOrCaretakerByIdWithSpecializationsAsync(int userId, int nurseryId);
    }
}
