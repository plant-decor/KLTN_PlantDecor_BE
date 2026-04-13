using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class LayoutDesignPlantRepository : GenericRepository<LayoutDesignPlant>, ILayoutDesignPlantRepository
    {
        public LayoutDesignPlantRepository(PlantDecorContext context) : base(context)
        {
        }
    }
}
