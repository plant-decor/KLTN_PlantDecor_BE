using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ITagRepository : IGenericRepository<Tag>
    {
        Task<bool> ExistsByNameAsync(string tagName, int? excludeId = null);
        Task<Tag?> GetByIdWithProductsAsync(int id);
        Task<List<Tag>> GetByIdsAsync(List<int> ids);
    }
}
