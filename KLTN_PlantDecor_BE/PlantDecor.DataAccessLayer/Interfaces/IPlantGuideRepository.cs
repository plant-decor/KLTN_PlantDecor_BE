using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IPlantGuideRepository : IGenericRepository<PlantGuide>
    {
        Task<PaginatedResult<PlantGuide>> GetAllWithPlantAsync(Pagination pagination);
        Task<PlantGuide?> GetByPlantIdWithPlantAsync(int plantId);
        Task<PlantGuide?> GetByIdWithPlantAsync(int id);
        Task<bool> ExistsByPlantIdAsync(int plantId, int? excludeId = null);
    }
}
