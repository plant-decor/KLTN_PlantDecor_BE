using PlantDecor.DataAccessLayer.Entities;
using System.Linq.Expressions;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ICategoryRepository : IGenericRepository<Category>
    {
        Task<List<Category>> GetAllWithParentAsync();
        Task<List<Category>> GetAllActiveWithParentAsync();
        Task<List<Category>> GetRootCategoriesWithChildrenAsync();
        Task<List<Category>> GetRootActiveCategoriesWithChildrenAsync();
        Task<Category?> GetByIdWithDetailsAsync(int id);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<bool> HasChildrenAsync(int id);
        Task<bool> HasProductsAsync(int id);
    }
}
