using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class LayoutDesignAiResponseImageRepository : GenericRepository<LayoutDesignAiResponseImage>, ILayoutDesignAiResponseImageRepository
    {
        public LayoutDesignAiResponseImageRepository(PlantDecorContext context) : base(context)
        {
        }
    }
}