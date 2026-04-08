using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IPlantInstanceRepository : IGenericRepository<PlantInstance>
    {
        /// <summary>
        /// Lấy danh sách PlantInstance theo NurseryId (có phân trang, include Plant & Images)
        /// </summary>
        Task<PaginatedResult<PlantInstance>> GetByNurseryIdAsync(int nurseryId, Pagination pagination, int? statusFilter = null);

        /// <summary>
        /// Lấy PlantInstance theo Id (include Plant, Nursery, Images)
        /// </summary>
        Task<PlantInstance?> GetByIdWithDetailsAsync(int id);
        Task<string?> GetPrimaryImageUrlAsync(int plantInstanceId);

        /// <summary>
        /// Lấy danh sách PlantInstance theo nhiều Ids
        /// </summary>
        Task<List<PlantInstance>> GetByIdsAsync(List<int> ids);

        /// <summary>
        /// Lấy danh sách PlantInstance theo NurseryId (không phân trang, cho summary)
        /// </summary>
        Task<List<PlantInstance>> GetAllByNurseryIdAsync(int nurseryId);

        /// <summary>
        /// Lấy danh sách Nursery có PlantInstance available cho một Plant cụ thể
        /// </summary>
        Task<List<PlantInstance>> GetAvailableByPlantIdAsync(int plantId);

        /// <summary>
        /// Batch add nhiều PlantInstance
        /// </summary>
        Task AddRangeAsync(IEnumerable<PlantInstance> instances);

        /// <summary>
        /// Lấy danh sách PlantInstance available theo NurseryId (Shop - phân trang)
        /// </summary>
        Task<PaginatedResult<PlantInstance>> GetAvailableByNurseryIdAsync(int nurseryId, Pagination pagination, int? plantId = null);

        /// <summary>
        /// Lấy danh sách PlantInstance available cho shop (toàn hệ thống hoặc theo nursery)
        /// </summary>
        Task<PaginatedResult<PlantInstance>> GetAvailableForShopAsync(Pagination pagination, int? nurseryId = null, int? plantId = null);

        Task<int> CountForEmbeddingBackfillAsync();
        Task<List<PlantInstance>> GetEmbeddingBackfillBatchAsync(int skip, int take);
        Task<List<PlantInstance>> GetByPlantIdForEmbeddingAsync(int plantId);
    }
}
