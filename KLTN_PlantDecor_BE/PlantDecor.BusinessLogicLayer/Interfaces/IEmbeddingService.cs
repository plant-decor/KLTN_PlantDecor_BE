using PlantDecor.DataAccessLayer.Entities;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IEmbeddingService
    {
        Task<List<Embedding>> CreateEmbeddingAsync<T>(T entity, Guid entityId, string entityType) where T : class;
        Task<List<Embedding>> SearchSimilarAsync(float[] queryVector, int limit = 10, string? entityType = null);
        Task<Embedding?> GetByEntityAsync(string entityType, Guid entityId);
        Task<bool> DeleteByEntityAsync(string entityType, Guid entityId);
        Task UpdateEmbeddingAsync<T>(T entity, Guid entityId, string entityType) where T : class;
    }
}
