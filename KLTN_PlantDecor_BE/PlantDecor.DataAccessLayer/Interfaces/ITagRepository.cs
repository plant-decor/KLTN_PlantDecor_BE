using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ITagRepository : IGenericRepository<Tag>
    {
        Task<PaginatedResult<Tag>> GetAllTagsWithPaginationAsync(Pagination pagination);
        Task<bool> ExistsByNameAsync(string tagName, int? excludeId = null);
        Task<Tag?> GetByIdWithProductsAsync(int id);
        Task<List<Tag>> GetByIdsAsync(List<int> ids);
    }
}
