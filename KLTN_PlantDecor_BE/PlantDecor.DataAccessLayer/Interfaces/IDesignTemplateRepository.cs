using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IDesignTemplateRepository : IGenericRepository<DesignTemplate>
    {
        Task<DesignTemplate?> GetByIdWithDetailsAsync(int id);
    }
}