namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface ILangflowService
    {
        Task<float[]?> GenerateEmbeddingAsync(string text);
        Task<string> IngestDataAsync<T>(T entity, Guid entityId, string entityType) where T : class;
    }
}
