using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ILayoutDesignAiResponseImageRepository : IGenericRepository<LayoutDesignAiResponseImage>
    {
        Task<List<LayoutDesignAiResponseImage>> GetByLayoutDesignIdAsync(int layoutDesignId);
    }
}