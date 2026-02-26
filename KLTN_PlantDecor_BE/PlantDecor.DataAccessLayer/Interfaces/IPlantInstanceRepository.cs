using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IPlantInstanceRepository : IGenericRepository<PlantInstance>
    {
        Task<PaginatedResult<PlantInstance>> GetAllWithPlantAsync(Pagination pagination);
        Task<PaginatedResult<PlantInstance>> GetByPlantIdAsync(int plantId, Pagination pagination);
        Task<PlantInstance?> GetByIdWithPlantAsync(int id);
        Task<PlantInstance?> GetByIdWithOrdersAsync(int id);
    }
}
