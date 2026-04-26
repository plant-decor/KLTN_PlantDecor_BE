using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IPolicyContentRepository : IGenericRepository<PolicyContent>
    {
        Task<List<PolicyContent>> GetAllActiveAsync();
        Task<List<PolicyContent>> GetByCategoryActiveAsync(int category);
        Task<List<PolicyContent>> GetAdminListAsync(bool includeInactive = true);
        Task<bool> ExistsByTitleInCategoryAsync(string title, int category, int? excludeId = null);
    }
}
