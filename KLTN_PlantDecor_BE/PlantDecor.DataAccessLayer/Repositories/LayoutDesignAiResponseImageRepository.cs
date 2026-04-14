using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class LayoutDesignAiResponseImageRepository : GenericRepository<LayoutDesignAiResponseImage>, ILayoutDesignAiResponseImageRepository
    {
        public LayoutDesignAiResponseImageRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<List<LayoutDesignAiResponseImage>> GetByLayoutDesignIdAsync(int layoutDesignId)
        {
            return await _context.LayoutDesignAiResponseImages
                .Where(image => image.LayoutDesignId == layoutDesignId)
                .OrderByDescending(image => image.CreatedAt)
                .ThenByDescending(image => image.Id)
                .ToListAsync();
        }
    }
}