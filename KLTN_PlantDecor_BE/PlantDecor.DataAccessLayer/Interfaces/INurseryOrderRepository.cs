using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface INurseryOrderRepository : IGenericRepository<NurseryOrder>
    {
        Task<List<NurseryOrder>> GetByNurseryIdAsync(int nurseryId);
    }
}
