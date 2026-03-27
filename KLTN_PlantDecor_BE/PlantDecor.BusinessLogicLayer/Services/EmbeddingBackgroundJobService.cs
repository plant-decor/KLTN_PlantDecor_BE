using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.DTOs.Embedding;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.BusinessLogicLayer.Services
{
    /// <summary>
    /// Background job service for processing embeddings via Hangfire
    /// Each method is called by Hangfire as a background job
    /// </summary>
    public class EmbeddingBackgroundJobService : IEmbeddingBackgroundJobService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly ILogger<EmbeddingBackgroundJobService> _logger;

        public EmbeddingBackgroundJobService(
            IEmbeddingService embeddingService,
            ILogger<EmbeddingBackgroundJobService> logger)
        {
            _embeddingService = embeddingService;
            _logger = logger;
        }

        public async Task ProcessCommonPlantEmbeddingAsync(CommonPlantEmbeddingDto dto, Guid entityId, string entityType)
        {
            await ProcessEmbeddingInternalAsync(dto, entityId, entityType);
        }

        public async Task ProcessPlantInstanceEmbeddingAsync(PlantInstanceEmbeddingDto dto, Guid entityId, string entityType)
        {
            await ProcessEmbeddingInternalAsync(dto, entityId, entityType);
        }

        public async Task ProcessNurseryPlantComboEmbeddingAsync(NurseryPlantComboEmbeddingDto dto, Guid entityId, string entityType)
        {
            await ProcessEmbeddingInternalAsync(dto, entityId, entityType);
        }

        public async Task ProcessNurseryMaterialEmbeddingAsync(NurseryMaterialEmbeddingDto dto, Guid entityId, string entityType)
        {
            await ProcessEmbeddingInternalAsync(dto, entityId, entityType);
        }

        private async Task ProcessEmbeddingInternalAsync<T>(T entity, Guid entityId, string entityType) where T : class
        {
            try
            {
                await _embeddingService.CreateEmbeddingAsync(entity, entityId, entityType);

                _logger.LogInformation($"Hangfire job completed: Created embedding for {entityType}:{entityId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Hangfire job failed: Error creating embedding for {entityType}:{entityId}");
                throw; // Re-throw to let Hangfire handle retries
            }
        }
    }
}
