using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface INurseryOrderRepository : IGenericRepository<NurseryOrder>
    {
        Task<List<NurseryOrder>> GetByNurseryIdAsync(int nurseryId);
        Task<List<NurseryOrder>> GetByShipperAndNurseryAsync(int shipperId, int nurseryId, List<int>? statuses = null);
        Task<NurseryOrder?> GetByIdWithDetailsAsync(int nurseryOrderId);
    }
}
