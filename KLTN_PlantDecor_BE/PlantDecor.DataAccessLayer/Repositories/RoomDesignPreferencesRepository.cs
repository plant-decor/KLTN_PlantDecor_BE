using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class RoomDesignPreferencesRepository : GenericRepository<RoomDesignPreferences>, IRoomDesignPreferencesRepository
    {
        public RoomDesignPreferencesRepository(PlantDecorContext context) : base(context)
        {
        }
    }
}
