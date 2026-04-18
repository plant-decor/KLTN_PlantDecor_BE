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
                .Include(image => image.LayoutDesignPlant)
                .Where(image => image.LayoutDesignId == layoutDesignId)
                .OrderByDescending(image => image.CreatedAt)
                .ThenByDescending(image => image.Id)
                .ToListAsync();
        }

        public async Task<List<LayoutDesignAiResponseImage>> GetAllGeneratedImagesByUserIdAsync(int userId)
        {
            return await _context.LayoutDesignAiResponseImages
                .Include(image => image.LayoutDesignPlant)
                .Where(image => image.LayoutDesign.UserId == userId)
                .OrderByDescending(image => image.CreatedAt)
                .ThenByDescending(image => image.Id)
                .ToListAsync();
        }
    }
}