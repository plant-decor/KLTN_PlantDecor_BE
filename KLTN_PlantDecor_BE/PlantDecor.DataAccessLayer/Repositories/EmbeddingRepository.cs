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
            var entityTypes = string.IsNullOrEmpty(entityType)
                ? null
                : new List<string> { entityType };

            return await SearchSimilarAsync(queryVector, limit, entityTypes);
        }

        public async Task<List<Embedding>> SearchSimilarAsync(Vector queryVector, int limit, IEnumerable<string>? entityTypes)
        {
            if (limit <= 0)
            {
                return new List<Embedding>();
            }

            var normalizedEntityTypes = entityTypes?
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var query = _context.Embeddings
                .AsNoTracking()
                .Where(e => e.EmbeddingVector != null);

            if (normalizedEntityTypes is { Count: > 0 })
            {
                query = query.Where(e => normalizedEntityTypes.Contains(e.EntityType));
            }

            // Chunked embeddings can produce duplicate entities in top-k.
            // Use adaptive top-K expansion (without OFFSET) and dedupe by entity.
            var scoredQuery = query
                .Select(e => new
                {
                    Embedding = e,
                    CosineSimilarity = 1.0 - e.EmbeddingVector!.CosineDistance(queryVector)
                })
                .OrderByDescending(x => x.CosineSimilarity);

            // dùng để tính size của batch fetch, bắt đầu với một giá trị hợp lý (ví dụ: 4 lần limit) và tăng dần nếu cần thiết để tìm đủ kết quả duy nhất, nhưng không vượt quá một ngưỡng tối đa để tránh truy vấn quá lớn.
            var fetchSize = Math.Max(50, limit * 4);
            const int maxFetchSize = 4000;
            const int maxAttempts = 6;

            var bestResults = new List<Embedding>();

            for (var attempt = 0; attempt < maxAttempts; attempt++)
            {
                var candidates = await scoredQuery
                    .Take(fetchSize)
                    .ToListAsync();

                // nếu không còn kết quả nào để mở rộng, trả về kết quả tốt nhất hiện tại (có thể là trống)
                if (candidates.Count == 0)
                {
                    return bestResults;
                }

                // dedupe theo entity (EntityType + EntityId) vì một thực thể có thể được chia thành nhiều chunk và do đó có nhiều embedding.
                // Chúng ta chỉ muốn một embedding đại diện cho mỗi thực thể trong kết quả cuối cùng.
                var deduped = new List<Embedding>(Math.Min(limit, candidates.Count));
                var seenEntities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var candidate in candidates)
                {
                    var embedding = candidate.Embedding;
                    var key = $"{embedding.EntityType}:{embedding.EntityId}";
                    if (!seenEntities.Add(key))
                    {
                        continue;
                    }

                    var metadata = embedding.Metadata != null
                        ? new Dictionary<string, object>(embedding.Metadata)
                        : new Dictionary<string, object>();
                    metadata["CosineSimilarityScore"] = candidate.CosineSimilarity;
                    embedding.Metadata = metadata;

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
