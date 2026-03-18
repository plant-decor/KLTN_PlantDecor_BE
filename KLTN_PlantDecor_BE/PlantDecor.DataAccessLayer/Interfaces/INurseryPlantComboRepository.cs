using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface INurseryPlantComboRepository : IGenericRepository<NurseryPlantCombo>
    {
        Task<NurseryPlantCombo?> GetByNurseryAndComboAsync(int nurseryId, int comboId);
        IQueryable<NurseryPlantCombo> GetQuery();
    }
}
