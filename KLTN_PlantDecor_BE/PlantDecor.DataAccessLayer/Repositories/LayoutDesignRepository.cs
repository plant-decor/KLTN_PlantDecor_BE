using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class LayoutDesignRepository : GenericRepository<LayoutDesign>, ILayoutDesignRepository
    {
        public LayoutDesignRepository(PlantDecorContext context) : base(context)
        {
        }
    }
}
