using Microsoft.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class NurseryPlantComboRepository : GenericRepository<NurseryPlantCombo>, INurseryPlantComboRepository
    {
        public NurseryPlantComboRepository(PlantDecorContext context) : base(context) { }

        public override async Task<NurseryPlantCombo> GetByIdAsync(int id)
        {
            return await _context.NurseryPlantCombos
                .Include(npc => npc.PlantCombo)
                .FirstOrDefaultAsync(npc => npc.Id == id);
        }

        public async Task<NurseryPlantCombo?> GetByNurseryAndComboAsync(int nurseryId, int comboId)
        {
            return await _context.NurseryPlantCombos
                .FirstOrDefaultAsync(npc => npc.NurseryId == nurseryId && npc.PlantComboId == comboId);
        }

        public IQueryable<NurseryPlantCombo> GetQuery()
        {
            return _context.NurseryPlantCombos.AsQueryable();
        }
        public async Task<NurseryPlantCombo?> GetByIdWithComboItemsAsync(int id)
        {
            return await _context.NurseryPlantCombos
                .Include(npc => npc.PlantCombo)
                    .ThenInclude(pc => pc.PlantComboItems)
                .FirstOrDefaultAsync(npc => npc.Id == id);
        }
    }
}
