using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface INurseryCareServiceRepository : IGenericRepository<NurseryCareService>
    {
        Task<NurseryCareService?> GetByIdWithDetailsAsync(int id);
        Task<List<NurseryCareService>> GetActiveByPackageIdAsync(int careServicePackageId);
        Task<List<NurseryCareService>> GetByNurseryIdAsync(int nurseryId);
        Task<List<NurseryCareService>> GetAllByNurseryIdAsync(int nurseryId);
        Task<bool> ExistsByNurseryAndPackageAsync(int nurseryId, int packageId, int? excludeId = null);
    }
}
