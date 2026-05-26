using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class UserAIUsageRepository : GenericRepository<UserAIUsage>, IUserAIUsageRepository
    {
        public UserAIUsageRepository(PlantDecorContext context) : base(context) { }
    }
}
