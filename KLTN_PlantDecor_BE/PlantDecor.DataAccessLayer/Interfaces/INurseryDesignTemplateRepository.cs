using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface INurseryDesignTemplateRepository : IGenericRepository<NurseryDesignTemplate>
    {
        Task<List<NurseryDesignTemplate>> GetByNurseryIdAsync(int nurseryId, bool activeOnly = true);
        Task<List<NurseryDesignTemplate>> GetByTemplateIdAsync(int designTemplateId, bool activeOnly = true);
        Task<List<int>> GetActiveDesignTemplateIdsAsync();
        Task<bool> ExistsByNurseryAndTemplateAsync(int nurseryId, int designTemplateId, int? excludeId = null);
    }
}