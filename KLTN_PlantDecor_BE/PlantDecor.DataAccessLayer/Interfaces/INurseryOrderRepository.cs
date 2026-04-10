using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface INurseryOrderRepository : IGenericRepository<NurseryOrder>
    {
        Task<List<NurseryOrder>> GetByNurseryIdAsync(int nurseryId);
        Task<List<NurseryOrder>> GetByShipperAndNurseryAsync(int shipperId, int nurseryId, List<int>? statuses = null);
        Task<NurseryOrder?> GetByIdWithDetailsAsync(int nurseryOrderId);
        Task<(List<NurseryOrder> Items, int TotalCount)> GetByShipperAndNurseryPagedAsync(int shipperId, int nurseryId, int? status, int skip, int take);
        Task<(List<NurseryOrder> Items, int TotalCount)> GetByNurseryIdPagedAsync(int nurseryId, int? status, int skip, int take);
    }
}
