using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IRoomImageRepository : IGenericRepository<RoomImage>
    {
        Task<List<RoomImage>> GetByUserAndIdsAsync(int userId, IReadOnlyCollection<int> roomImageIds);
    }
}
