using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class RoomImageRepository : GenericRepository<RoomImage>, IRoomImageRepository
    {
        public RoomImageRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<List<RoomImage>> GetByUserAndIdsAsync(int userId, IReadOnlyCollection<int> roomImageIds)
        {
            if (roomImageIds == null || roomImageIds.Count == 0)
            {
                return new List<RoomImage>();
            }

            var normalizedIds = roomImageIds
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (normalizedIds.Count == 0)
            {
                return new List<RoomImage>();
            }

            return await _context.RoomImages
                .Where(roomImage => roomImage.UserId == userId && normalizedIds.Contains(roomImage.Id))
                .ToListAsync();
        }

        public async Task<List<RoomImage>> GetAllByUserIdAsync(int userId)
        {
            return await _context.RoomImages
                .AsNoTracking()
                .Include(roomImage => roomImage.RoomUploadModerations)
                .Where(roomImage => roomImage.UserId == userId)
                .OrderByDescending(roomImage => roomImage.UploadedAt)
                .ThenByDescending(roomImage => roomImage.Id)
                .ToListAsync();
        }

        public async Task<List<RoomImage>> GetAllByUserIdAndViewAngleAsync(int userId, int viewAngle)
        {
            return await _context.RoomImages
                .AsNoTracking()
                .Include(roomImage => roomImage.RoomUploadModerations)
                .Where(roomImage => roomImage.UserId == userId && roomImage.ViewAngle == viewAngle)
                .OrderByDescending(roomImage => roomImage.UploadedAt)
                .ThenByDescending(roomImage => roomImage.Id)
                .ToListAsync();
        }
    }
}
