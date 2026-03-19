using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface INurseryPlantComboRepository : IGenericRepository<NurseryPlantCombo>
    {
        Task<NurseryPlantCombo?> GetByIdWithComboItemsAsync(int id);
    }
}
