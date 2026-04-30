using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface INurseryPlantComboRepository : IGenericRepository<NurseryPlantCombo>
    {
        Task<NurseryPlantCombo?> GetByNurseryAndComboAsync(int nurseryId, int comboId);
        IQueryable<NurseryPlantCombo> GetQuery();
        Task<NurseryPlantCombo?> GetByIdWithComboItemsAsync(int id);
        Task<int> CountForEmbeddingBackfillAsync();
        Task<List<NurseryPlantCombo>> GetEmbeddingBackfillBatchAsync(int skip, int take);
        Task<List<NurseryPlantCombo>> GetByPlantComboIdForEmbeddingAsync(int comboId);
    }
}
