using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IUserRepository : IGenericRepository<User>
    {
        Task SetActive(User user);
        Task DeActive(User user);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByRefreshTokenAsync(string refreshToken);
        Task<bool> VerifyPasswordAsync(User user, string password);
        Task<List<RefreshToken>> GetRefreshTokenAsync(int userId);
        Task<User> GetByPhoneAsync(string phone);
        Task<bool> IsVerifiedAsync(int userId);
    }
}
