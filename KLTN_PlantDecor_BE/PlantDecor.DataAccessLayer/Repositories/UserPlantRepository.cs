using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class UserPlantRepository : GenericRepository<UserPlant>, IUserPlantRepository
    {
        public UserPlantRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<List<UserPlant>> GetByUserIdWithDetailsAsync(int userId)
        {
            return await _context.UserPlants
                .AsNoTracking()
                .Where(userPlant => userPlant.UserId == userId)
                .Include(userPlant => userPlant.Plant)
                    .ThenInclude(plant => plant!.PlantImages)
                .Include(userPlant => userPlant.PlantInstance)
                    .ThenInclude(instance => instance!.Plant)
                .Include(userPlant => userPlant.PlantInstance)
                    .ThenInclude(instance => instance!.PlantImages)
                .OrderByDescending(userPlant => userPlant.CreatedAt)
                .ThenByDescending(userPlant => userPlant.Id)
                .ToListAsync();
        }
    }
}