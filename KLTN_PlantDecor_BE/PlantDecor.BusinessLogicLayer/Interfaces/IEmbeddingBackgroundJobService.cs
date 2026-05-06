using PlantDecor.BusinessLogicLayer.DTOs.Embedding;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    /// <summary>
    /// Service for processing embeddings in background using Hangfire
    /// </summary>
    public interface IEmbeddingBackgroundJobService
    {
        // CommonPlant embeddings
        Task ProcessCommonPlantEmbeddingAsync(CommonPlantEmbeddingDto dto, Guid entityId, string entityType);

        // PlantInstance embeddings
        Task ProcessPlantInstanceEmbeddingAsync(PlantInstanceEmbeddingDto dto, Guid entityId, string entityType);

        // NurseryPlantCombo embeddings
        Task ProcessNurseryPlantComboEmbeddingAsync(NurseryPlantComboEmbeddingDto dto, Guid entityId, string entityType);

        // NurseryMaterial embeddings
        Task ProcessNurseryMaterialEmbeddingAsync(NurseryMaterialEmbeddingDto dto, Guid entityId, string entityType);

        // CareServicePackage embeddings
        Task ProcessCareServicePackageEmbeddingAsync(CareServicePackageEmbeddingDto dto, Guid entityId, string entityType);

        // Backfill orchestration
        Task QueueBackfillAllAsync(int batchSize);
        Task QueueBackfillByEntityTypeAsync(string entityType, int batchSize);
        Task ProcessBackfillBatchAsync(string entityType, int pageNumber, int pageSize);
    }
}
