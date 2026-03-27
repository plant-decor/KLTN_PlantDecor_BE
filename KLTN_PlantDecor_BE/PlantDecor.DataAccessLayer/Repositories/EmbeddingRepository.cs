using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using PlantDecor.DataAccessLayer.Context;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Interfaces;

namespace PlantDecor.DataAccessLayer.Repositories
{
    public class EmbeddingRepository : GenericRepository<Embedding>, IEmbeddingRepository
    {
        public EmbeddingRepository(PlantDecorContext context) : base(context)
        {
        }

        public async Task<List<Embedding>> GetByEntityTypeAsync(string entityType)
        {
            return await _context.Embeddings
                .Where(e => e.EntityType == entityType)
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }

        public async Task<Embedding?> GetByEntityAsync(string entityType, Guid entityId)
        {
            return await _context.Embeddings
                .FirstOrDefaultAsync(e => e.EntityType == entityType && e.EntityId == entityId);
        }

        public async Task<List<Embedding>> SearchSimilarAsync(Vector queryVector, int limit = 10, string? entityType = null)
        {
            var query = _context.Embeddings
                .Where(e => e.EmbeddingVector != null);

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(e => e.EntityType == entityType);
            }

            // Sử dụng L2Distance hoặc có thể thay bằng CosineDistance nếu cần
            return await query
                .OrderBy(e => e.EmbeddingVector!.L2Distance(queryVector))
                .Take(limit)
                .ToListAsync();
        }

        public async Task<bool> DeleteByEntityAsync(string entityType, Guid entityId)
        {
            var embedding = await GetByEntityAsync(entityType, entityId);
            if (embedding != null)
            {
                return await RemoveAsync(embedding);
            }
            return false;
        }

        public async Task AddRangeAsync(IEnumerable<Embedding> embeddings)
        {
            await _context.Embeddings.AddRangeAsync(embeddings);
            await _context.SaveChangesAsync();
        }
    }
}
