using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ILayoutDesignAiResponseImageRepository : IGenericRepository<LayoutDesignAiResponseImage>
    {
        Task<List<LayoutDesignAiResponseImage>> GetByLayoutDesignIdAsync(int layoutDesignId);
        Task<List<LayoutDesignAiResponseImage>> GetAllGeneratedImagesByUserIdAsync(int userId);
    }
}