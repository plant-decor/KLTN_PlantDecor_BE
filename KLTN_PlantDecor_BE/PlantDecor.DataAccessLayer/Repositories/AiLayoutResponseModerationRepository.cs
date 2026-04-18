using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class AiLayoutResponseModerationRepository : GenericRepository<AilayoutResponseModeration>, IAiLayoutResponseModerationRepository
    {
        public AiLayoutResponseModerationRepository(PlantDecorContext context) : base(context)
        {
        }
    }
}
