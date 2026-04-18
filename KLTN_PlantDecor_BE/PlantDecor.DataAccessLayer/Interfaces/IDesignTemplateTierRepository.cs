using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IDesignTemplateTierRepository : IGenericRepository<DesignTemplateTier>
    {
        Task<DesignTemplateTier?> GetByIdWithItemsAsync(int id);
        Task<List<DesignTemplateTier>> GetByTemplateIdWithItemsAsync(int designTemplateId, bool activeOnly = true);
    }
}