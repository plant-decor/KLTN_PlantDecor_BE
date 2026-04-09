using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class RoomImageRepository : GenericRepository<RoomImage>, IRoomImageRepository
    {
        public RoomImageRepository(PlantDecorContext context) : base(context)
        {
        }
    }
}
