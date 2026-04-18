using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class TaskMaterialUsageRepository : GenericRepository<TaskMaterialUsage>, ITaskMaterialUsageRepository
    {
        public TaskMaterialUsageRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<List<TaskMaterialUsage>> GetByTaskIdAsync(int designTaskId)
        {
            return await _context.TaskMaterialUsages
                .AsNoTracking()
                .Include(x => x.Material)
                .Where(x => x.DesignTaskId == designTaskId)
                .OrderBy(x => x.Id)
                .ToListAsync();
        }
    }
}