using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IDesignTemplateSpecializationRepository : IGenericRepository<DesignTemplateSpecialization>
    {
        Task<List<DesignTemplateSpecialization>> GetByTemplateIdAsync(int designTemplateId);
        Task<List<DesignTemplateSpecialization>> GetBySpecializationIdAsync(int specializationId);
        Task<bool> ExistsAsync(int designTemplateId, int specializationId);
    }
}