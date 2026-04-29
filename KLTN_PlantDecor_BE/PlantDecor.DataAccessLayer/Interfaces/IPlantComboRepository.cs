using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IPlantComboRepository : IGenericRepository<PlantCombo>
    {
        Task<PaginatedResult<PlantCombo>> GetAllWithDetailsAsync(Pagination pagination);
        Task<PaginatedResult<PlantCombo>> GetActiveWithDetailsAsync(Pagination pagination);
        Task<PlantCombo?> GetByIdWithDetailsAsync(int id);
        Task<PlantCombo?> GetByIdWithOrdersAsync(int id);
        Task<bool> ExistsByCodeAsync(string comboCode, int? excludeId = null);
        Task<bool> ExistsByNameAsync(string name, int? excludeId = null);
        Task<PaginatedResult<PlantCombo>> GetCombosForShopAsync(Pagination pagination);
        Task<PlantCombo?> GetComboByComboItemIdAsync(int comboItemId);
        Task<List<PlantCombo>> GetCompatibleCombosForNurseryAsync(int nurseryId);
    }
}
