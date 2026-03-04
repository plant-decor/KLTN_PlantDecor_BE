using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface INurseryMaterialRepository : IGenericRepository<NurseryMaterial>
    {
        Task<PaginatedResult<NurseryMaterial>> GetAllWithDetailsAsync(Pagination pagination);
        Task<NurseryMaterial?> GetByIdWithDetailsAsync(int id);
        Task<PaginatedResult<NurseryMaterial>> GetByNurseryIdAsync(int nurseryId, Pagination pagination);
        Task<PaginatedResult<NurseryMaterial>> GetByMaterialIdAsync(int materialId, Pagination pagination);
        Task<NurseryMaterial?> GetByMaterialAndNurseryAsync(int materialId, int nurseryId);
        Task<bool> ExistsAsync(int materialId, int nurseryId, int? excludeId = null);
    }
}
