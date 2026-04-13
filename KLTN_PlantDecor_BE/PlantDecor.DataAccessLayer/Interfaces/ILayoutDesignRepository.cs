using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ILayoutDesignRepository : IGenericRepository<LayoutDesign>
    {
        Task<LayoutDesign?> GetGenerationContextByIdAsync(int layoutDesignId);
        Task<int?> GetOwnerIdAsync(int layoutDesignId);
    }
}
