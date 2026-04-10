using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.DTOs.Embedding;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.BusinessLogicLayer.Services
{
    /// <summary>
    /// Background job service for pushing data to Langflow via Hangfire
    /// Each method is called by Hangfire as a background job
    /// </summary>
    public class LangflowBackgroundJobService : ILangflowBackgroundJobService
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<LangflowBackgroundJobService> _logger;

        public LangflowBackgroundJobService(
            IServiceScopeFactory serviceScopeFactory,
            ILogger<LangflowBackgroundJobService> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task ProcessCommonPlantIngestionAsync(CommonPlantEmbeddingDto dto, Guid entityId, string entityType)
        {
            await ProcessIngestionInternalAsync(dto, entityId, entityType);
        }

        public async Task ProcessPlantInstanceIngestionAsync(PlantInstanceEmbeddingDto dto, Guid entityId, string entityType)
        {
            await ProcessIngestionInternalAsync(dto, entityId, entityType);
        }

        public async Task ProcessNurseryPlantComboIngestionAsync(NurseryPlantComboEmbeddingDto dto, Guid entityId, string entityType)
        {
            await ProcessIngestionInternalAsync(dto, entityId, entityType);
        }

        public async Task ProcessNurseryMaterialIngestionAsync(NurseryMaterialEmbeddingDto dto, Guid entityId, string entityType)
        {
            await ProcessIngestionInternalAsync(dto, entityId, entityType);
        }

        private async Task ProcessIngestionInternalAsync<T>(T entity, Guid entityId, string entityType) where T : class
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var langflowService = scope.ServiceProvider.GetRequiredService<ILangflowService>();

                await langflowService.IngestDataAsync(entity, entityId, entityType);

                _logger.LogInformation($"Hangfire job completed: Ingested data to Langflow for {entityType}:{entityId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Hangfire job failed: Error ingesting data to Langflow for {entityType}:{entityId}");
                throw; // Re-throw to let Hangfire handle retries
            }
        }
    }
}