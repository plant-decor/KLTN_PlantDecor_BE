using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface ILayoutDesignRepository : IGenericRepository<LayoutDesign>
    {
        Task<PaginatedResult<LayoutDesign>> GetAllByUserIdWithDetailsAsync(int userId, Pagination pagination);
        Task<LayoutDesign?> GetGenerationContextByIdAsync(int layoutDesignId);
        Task<int?> GetOwnerIdAsync(int layoutDesignId);
    }
}
