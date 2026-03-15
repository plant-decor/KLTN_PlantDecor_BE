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

    }
}
