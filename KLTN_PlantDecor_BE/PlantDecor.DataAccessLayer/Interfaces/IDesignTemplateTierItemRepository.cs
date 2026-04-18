using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IDesignTemplateTierItemRepository : IGenericRepository<DesignTemplateTierItem>
    {
        Task<List<DesignTemplateTierItem>> GetByTierIdAsync(int designTemplateTierId);
    }
}