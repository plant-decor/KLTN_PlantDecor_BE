using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IRoomImageRepository : IGenericRepository<RoomImage>
    {
        Task<List<RoomImage>> GetByUserAndIdsAsync(int userId, IReadOnlyCollection<int> roomImageIds);
        Task<List<RoomImage>> GetAllByUserIdAsync(int userId);
        Task<List<RoomImage>> GetAllByUserIdAndViewAngleAsync(int userId, int viewAngle);
    }
}
