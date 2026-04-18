using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ITaskMaterialUsageRepository : IGenericRepository<TaskMaterialUsage>
    {
        Task<List<TaskMaterialUsage>> GetByTaskIdAsync(int designTaskId);
    }
}