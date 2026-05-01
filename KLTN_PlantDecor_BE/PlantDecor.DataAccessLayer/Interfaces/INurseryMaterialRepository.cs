using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface INurseryMaterialRepository : IGenericRepository<NurseryMaterial>
    {
        Task<PaginatedResult<NurseryMaterial>> GetAllWithDetailsAsync(Pagination pagination);
        Task<NurseryMaterial?> GetByIdWithDetailsAsync(int id);
        Task<PaginatedResult<NurseryMaterial>> GetByNurseryIdAsync(int nurseryId, Pagination pagination);
        Task<List<NurseryMaterial>> GetAllByNurseryIdAsync(int nurseryId);
        Task<PaginatedResult<NurseryMaterial>> GetByMaterialIdAsync(int materialId, Pagination pagination);
        Task<NurseryMaterial?> GetByMaterialAndNurseryAsync(int materialId, int nurseryId);
        Task<bool> ExistsAsync(int materialId, int nurseryId, int? excludeId = null);

        Task<PaginatedResult<NurseryMaterial>> SearchForShopAsync(
            Pagination pagination,
            int? nurseryId,
            string? searchTerm,
            List<int>? categoryIds,
            List<int>? tagIds,
            double? minPrice,
            double? maxPrice,
            NurseryMaterialSortByEnum? sortBy,
            SortDirectionEnum? sortDirection);

        Task<int> CountForEmbeddingBackfillAsync();
        Task<List<NurseryMaterial>> GetEmbeddingBackfillBatchAsync(int skip, int take);
        Task<List<NurseryMaterial>> GetByMaterialIdForEmbeddingAsync(int materialId);
    }
}
