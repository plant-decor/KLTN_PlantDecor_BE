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
                .Include(u => u.WorkingNursery) // Include WorkingNursery for UserResponse
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        public override async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.UserProfile)
                .Include(u => u.RefreshTokens)
                .Include(u => u.WorkingNursery) // Include WorkingNursery for UserResponse
                .ToListAsync();
        }

        public override async Task<PaginatedResult<User>> GetAllAsyncWithPagination(Pagination pagination)
        {
            // Exclude Admin users in both count and items
            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.UserProfile)
                .Include(u => u.RefreshTokens)
                .Include(u => u.WorkingNursery); // Include WorkingNursery for UserResponse
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
            .Include(u => u.WorkingNursery) // Include WorkingNursery for UserResponse
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        // Get all shippers working in a specific nursery
        public async Task<List<User>> GetShippersByNurseryIdAsync(int nurseryId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.WorkingNursery)
                .Where(u =>
                    u.NurseryId == nurseryId &&
                    u.RoleId == (int)RoleEnum.Shipper &&
                    u.Status == (int)UserStatusEnum.Active &&
                    u.IsVerified)
                .ToListAsync();
        }

        public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
                return null;

            return await _context.Users
            .Include(u => u.Role)
            .Include(u => u.UserProfile)
            .Include(u => u.RefreshTokens)
            .Include(u => u.WorkingNursery) // Include WorkingNursery for UserResponse
            .FirstOrDefaultAsync(u =>
                    u.RefreshTokens.Any(rt => rt.Token.Equals(refreshToken) && rt.IsRevoked != true) &&
                    u.Status == (int)UserStatusEnum.Active
           );

        }

        public async Task<bool> VerifyPasswordAsync(User user, string password)
        {
            return await Task.FromResult(BCrypt.Net.BCrypt.Verify(password, user.PasswordHash));
        }

        public async Task<User?> GetByPhoneAsync(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.UserProfile)
                .Include(u => u.RefreshTokens)
                .Include(u => u.WorkingNursery) // Include WorkingNursery for UserResponse
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

        public async Task<bool> IsEmailExistsForOtherUserAsync(string email, int currentUserId)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var users = await GetAllAsync();
                return users.Any(u => u.Email != null &&
                                     u.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
                                     u.Id != currentUserId);
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<RefreshToken>?> GetOldRefreshTokenByDeviceIdAsync(int userId, string deviceId)
        {
            return await _context.RefreshTokens
                     .Where(t => t.DeviceId == deviceId && t.UserId == userId && t.IsRevoked != true)
                     .ToListAsync();
        }

        public async Task<RefreshToken?> GetRefreshTokenByDeviceIdAsync(int userId, string deviceId)
        {
            return await _context.RefreshTokens
                     .FirstOrDefaultAsync(t => t.DeviceId == deviceId && t.UserId == userId && t.IsRevoked != true);
        }

        public async Task<RefreshToken?> GetRefreshTokenByRefreshTokenAsync(int userId, string refreshToken)
        {
            return await _context.RefreshTokens
                     .FirstOrDefaultAsync(t => t.Token == refreshToken && t.UserId == userId && t.IsRevoked != true);
        }

        public async Task<int> DeleteRevokedRefreshTokensAsync()
        {
            var revokedTokens = await _context.RefreshTokens
                .Where(rt => rt.IsRevoked)
                .ToListAsync();

            if (revokedTokens.Count == 0) return 0;

            _context.RefreshTokens.RemoveRange(revokedTokens);
            return await _context.SaveChangesAsync();
        }

        public async Task<List<User>> GetCaretakersByNurseryIdAsync(int nurseryId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.UserProfile)
                .Include(u => u.StaffSpecializations)
                    .ThenInclude(ss => ss.Specialization)
                .Where(u => u.NurseryId == nurseryId && u.RoleId == (int)RoleEnum.Caretaker)
                .OrderBy(u => u.Username)
                .ToListAsync();
        }

        public async Task<User?> GetCaretakerByIdWithSpecializationsAsync(int userId, int nurseryId)
        {
            return await _context.Users
                .Include(u => u.Role)
                .Include(u => u.UserProfile)
                .Include(u => u.StaffSpecializations)
                    .ThenInclude(ss => ss.Specialization)
                .FirstOrDefaultAsync(u => u.Id == userId && u.NurseryId == nurseryId && u.RoleId == (int)RoleEnum.Caretaker);
        }
    }
}