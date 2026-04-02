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
                .Where(e => e.EntityType == entityType && e.EntityId == entityId)
                .OrderBy(e => e.ChunkIndex)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Embedding>> SearchSimilarAsync(Vector queryVector, int limit = 10, string? entityType = null)
        {
            if (limit <= 0)
            {
                return new List<Embedding>();
            }

            var query = _context.Embeddings
                .Where(e => e.EmbeddingVector != null);

            if (!string.IsNullOrEmpty(entityType))
            {
                query = query.Where(e => e.EntityType == entityType);
            }

            // Chunked embeddings can produce duplicate entities in top-k.
            // Use adaptive top-K expansion (without OFFSET) and dedupe by entity.
            var orderedQuery = query.OrderBy(e => e.EmbeddingVector!.L2Distance(queryVector));
            var fetchSize = Math.Max(50, limit * 4);
            const int maxFetchSize = 4000;
            const int maxAttempts = 6;

            var bestResults = new List<Embedding>();

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var candidates = await orderedQuery
                    .Take(fetchSize)
                    .ToListAsync();

                if (candidates.Count == 0)
                {
                    return bestResults;
                }

                var deduped = new List<Embedding>(Math.Min(limit, candidates.Count));
                var seenEntities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var embedding in candidates)
                {
                    var key = $"{embedding.EntityType}:{embedding.EntityId}";
                    if (!seenEntities.Add(key))
                    {
                        continue;
                    }

                    deduped.Add(embedding);
                    if (deduped.Count >= limit)
                    {
                        return deduped;
                    }
                }

                bestResults = deduped;

                // No more rows left to expand or we've reached safety cap.
                if (candidates.Count < fetchSize || fetchSize >= maxFetchSize)
                {
                    break;
                }

                fetchSize = Math.Min(fetchSize * 2, maxFetchSize);
            }

            return bestResults;
        }

        public async Task<bool> DeleteByEntityAsync(string entityType, Guid entityId)
        {
            var embeddings = await _context.Embeddings
                .Where(e => e.EntityType == entityType && e.EntityId == entityId)
                .ToListAsync();

            if (embeddings.Count == 0)
            {
                return false;
            }

            _context.Embeddings.RemoveRange(embeddings);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task AddRangeAsync(IEnumerable<Embedding> embeddings)
        {
            // không cần SaveChangesAsync ở đây vì chúng ta sẽ gọi SaveAsync ở UnitOfWork sau khi thêm tất cả các embedding
            await _context.Embeddings.AddRangeAsync(embeddings);
        }
    }
}
