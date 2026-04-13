using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IShiftRepository : IGenericRepository<Shift>
    {
        Task<bool> ExistsAsync(int id);
    }
}
