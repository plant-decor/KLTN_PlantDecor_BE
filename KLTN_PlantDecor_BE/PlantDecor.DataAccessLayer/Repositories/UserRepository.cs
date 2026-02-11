using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task SetActive(User user)
        {
            // user.IsActive = !user.IsActive;
            user.Status = (int)UserStatusEnum.Active;
            await UpdateAsync(user);
        }

        public async Task DeActive(User user)
        {
            user.Status = (int)UserStatusEnum.Inactive;
            await UpdateAsync(user);
        }

        public override async Task<User> GetByIdAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.UserProfile) // Include UserProfile for GetByIdAsync
                .Include(u => u.RefreshTokens) // Include RefreshTokens for GetByIdAsync
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public override async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.UserProfile)
                .Include(u => u.RefreshTokens)
                .ToListAsync();
        }

        public override async Task<PaginatedResult<User>> GetAllAsyncWithPagination(Pagination pagination)
        {
            // Exclude Admin users in both count and items
            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.UserProfile)
                .Include(u => u.RefreshTokens);
            //.Where(u => u.Role.Name != "Admin");

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderBy(u => u.Id)
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            return new PaginatedResult<User>(items, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            return await _context.Users
            .Include(u => u.Role)
            .Include(u => u.UserProfile)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            return await _context.Users
            .Include(u => u.Role)
            .Include(u => u.UserProfile)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u =>
                    u.RefreshTokens.Any(rt => rt.Token.Equals(refreshToken) && rt.IsRevoked != true) &&
                    u.Status == (int)UserStatusEnum.Active
           );

        }

        public async Task<bool> VerifyPasswordAsync(User user, string password)
        {
            return await Task.FromResult(BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));
        }

        public async Task<User> GetByPhoneAsync(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.UserProfile)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.PhoneNumber == phone);
        }

        public async Task<bool> IsVerifiedAsync(int userId)
        {
            var result = await _context.Users
                .AnyAsync(u => u.Id == userId && u.IsVerified);

            return result;
        }

        public async Task<List<RefreshToken>> GetRefreshTokenAsync(int userId)
        {

            return await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.IsRevoked != true)
                .ToListAsync();
        }
    }
}

