using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IPlantInstanceRepository : IGenericRepository<PlantInstance>
    {
        Task<List<PlantInstance>> GetAllWithPlantAsync();
        Task<List<PlantInstance>> GetByPlantIdAsync(int plantId);
        Task<PlantInstance?> GetByIdWithPlantAsync(int id);
        Task<PlantInstance?> GetByIdWithOrdersAsync(int id);
    }
}
