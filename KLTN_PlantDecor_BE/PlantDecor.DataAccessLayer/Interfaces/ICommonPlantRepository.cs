using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ICommonPlantRepository : IGenericRepository<CommonPlant>
    {
        Task<PaginatedResult<CommonPlant>> GetAllWithDetailsAsync(Pagination pagination);
        Task<CommonPlant?> GetByIdWithDetailsAsync(int id);
        Task<PaginatedResult<CommonPlant>> GetByPlantIdAsync(int plantId, Pagination pagination);
        Task<PaginatedResult<CommonPlant>> GetByNurseryIdAsync(int nurseryId, Pagination pagination);
        Task<CommonPlant?> GetByPlantAndNurseryAsync(int plantId, int nurseryId);
        Task<bool> ExistsAsync(int plantId, int nurseryId, int? excludeId = null);

        /// <summary>
        /// Lấy danh sách CommonPlant active theo NurseryId (Shop - phân trang)
        /// </summary>
        Task<PaginatedResult<CommonPlant>> GetActiveByNurseryIdAsync(int nurseryId, Pagination pagination);

        /// <summary>
        /// Lấy danh sách CommonPlant active theo PlantId (Shop)
        /// </summary>
        Task<List<CommonPlant>> GetActiveByPlantIdAsync(int plantId);
    }
}
