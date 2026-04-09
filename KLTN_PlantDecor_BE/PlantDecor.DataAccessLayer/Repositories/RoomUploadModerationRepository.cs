using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class RoomUploadModerationRepository : GenericRepository<RoomUploadModeration>, IRoomUploadModerationRepository
    {
        public RoomUploadModerationRepository(PlantDecorContext context) : base(context)
        {
        }
    }
}
