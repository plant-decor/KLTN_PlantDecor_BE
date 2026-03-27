using Pgvector;
using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.DataAccessLayer.Interfaces
{
    public interface IEmbeddingRepository : IGenericRepository<Embedding>
    {
        Task<List<Embedding>> GetByEntityTypeAsync(string entityType);
        Task<Embedding?> GetByEntityAsync(string entityType, Guid entityId);
        Task<List<Embedding>> SearchSimilarAsync(Vector queryVector, int limit = 10, string? entityType = null);
        Task<bool> DeleteByEntityAsync(string entityType, Guid entityId);
        Task AddRangeAsync(IEnumerable<Embedding> embeddings);
    }
}
