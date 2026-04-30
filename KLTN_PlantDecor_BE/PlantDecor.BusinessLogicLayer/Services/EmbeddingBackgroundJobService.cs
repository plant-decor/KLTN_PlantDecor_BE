using Hangfire;
using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.Constants;
using PlantDecor.BusinessLogicLayer.DTOs.Embedding;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    /// <summary>
    /// Background job service for processing embeddings via Hangfire
    /// Each method is called by Hangfire as a background job
    /// </summary>
    public class EmbeddingBackgroundJobService : IEmbeddingBackgroundJobService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ILogger<EmbeddingBackgroundJobService> _logger;

        public EmbeddingBackgroundJobService(
            IEmbeddingService embeddingService,
            IUnitOfWork unitOfWork,
            IBackgroundJobClient backgroundJobClient,
            ILogger<EmbeddingBackgroundJobService> logger)
        {
            _embeddingService = embeddingService;
            _unitOfWork = unitOfWork;
            _backgroundJobClient = backgroundJobClient;
            _logger = logger;
        }

        public async Task ProcessCommonPlantEmbeddingAsync(CommonPlantEmbeddingDto dto, Guid entityId, string entityType)
        {
            await EnrichPlantGuideAsync(dto);
            await ProcessEmbeddingInternalAsync(dto, entityId, entityType);
        }

        public async Task ProcessPlantInstanceEmbeddingAsync(PlantInstanceEmbeddingDto dto, Guid entityId, string entityType)
        {
            await EnrichPlantGuideAsync(dto);
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

        public async Task QueueBackfillAllAsync(int batchSize)
        {
            var normalizedBatchSize = NormalizeBatchSize(batchSize);

            foreach (var entityType in EmbeddingEntityTypes.AllTypes)
            {
                await QueueBackfillByEntityTypeAsync(entityType, normalizedBatchSize);
            }
        }

        public async Task QueueBackfillByEntityTypeAsync(string entityType, int batchSize)
        {
            if (!EmbeddingEntityTypes.IsValidType(entityType))
            {
                throw new ArgumentException($"Invalid embedding entity type: {entityType}", nameof(entityType));
            }

            var normalizedBatchSize = NormalizeBatchSize(batchSize);
            var totalCount = await GetTotalCountAsync(entityType);
            if (totalCount == 0)
            {
                _logger.LogInformation("Backfill skipped for {EntityType}: no records found", entityType);
                return;
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)normalizedBatchSize);

            for (var pageNumber = 1; pageNumber <= totalPages; pageNumber++)
            {
                var capturedPage = pageNumber;
                _backgroundJobClient.Enqueue<IEmbeddingBackgroundJobService>(
                    service => service.ProcessBackfillBatchAsync(entityType, capturedPage, normalizedBatchSize));
            }

            _logger.LogInformation(
                "Queued backfill for {EntityType}: {TotalCount} records, {TotalPages} batch jobs, batchSize={BatchSize}",
                entityType,
                totalCount,
                totalPages,
                normalizedBatchSize);
        }

        public async Task ProcessBackfillBatchAsync(string entityType, int pageNumber, int pageSize)
        {
            if (!EmbeddingEntityTypes.IsValidType(entityType))
            {
                throw new ArgumentException($"Invalid embedding entity type: {entityType}", nameof(entityType));
            }

            if (pageNumber <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageNumber), "Page number must be greater than 0.");
            }

            var normalizedPageSize = NormalizeBatchSize(pageSize);
            var skip = (pageNumber - 1) * normalizedPageSize;

            var processedCount = entityType switch
            {
                var t when t == EmbeddingEntityTypes.CommonPlant => await ProcessCommonPlantBackfillBatchAsync(skip, normalizedPageSize),
                var t when t == EmbeddingEntityTypes.PlantInstance => await ProcessPlantInstanceBackfillBatchAsync(skip, normalizedPageSize),
                var t when t == EmbeddingEntityTypes.NurseryPlantCombo => await ProcessNurseryPlantComboBackfillBatchAsync(skip, normalizedPageSize),
                var t when t == EmbeddingEntityTypes.NurseryMaterial => await ProcessNurseryMaterialBackfillBatchAsync(skip, normalizedPageSize),
                _ => 0
            };

            _logger.LogInformation(
                "Backfill batch completed for {EntityType}: page={PageNumber}, pageSize={PageSize}, processed={ProcessedCount}",
                entityType,
                pageNumber,
                normalizedPageSize,
                processedCount);
        }

        private async Task ProcessEmbeddingInternalAsync<T>(T entity, Guid entityId, string entityType) where T : class
        {
            try
            {
                await _embeddingService.UpdateEmbeddingAsync(entity, entityId, entityType);

                _logger.LogInformation($"Hangfire job completed: Updated embedding for {entityType}:{entityId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Hangfire job failed: Error updating embedding for {entityType}:{entityId}");
                throw; // Re-throw to let Hangfire handle retries
            }
        }

        private async Task<int> GetTotalCountAsync(string entityType)
        {
            return entityType switch
            {
                var t when t == EmbeddingEntityTypes.CommonPlant => await _unitOfWork.CommonPlantRepository.CountForEmbeddingBackfillAsync(),
                var t when t == EmbeddingEntityTypes.PlantInstance => await _unitOfWork.PlantInstanceRepository.CountForEmbeddingBackfillAsync(),
                var t when t == EmbeddingEntityTypes.NurseryPlantCombo => await _unitOfWork.NurseryPlantComboRepository.CountForEmbeddingBackfillAsync(),
                var t when t == EmbeddingEntityTypes.NurseryMaterial => await _unitOfWork.NurseryMaterialRepository.CountForEmbeddingBackfillAsync(),
                _ => 0
            };
        }

        private async Task<int> ProcessCommonPlantBackfillBatchAsync(int skip, int take)
        {
            var items = await _unitOfWork.CommonPlantRepository.GetEmbeddingBackfillBatchAsync(skip, take);

            var processed = 0;
            foreach (var entity in items)
            {
                try
                {
                    await _embeddingService.UpdateEmbeddingAsync(
                        entity.ToEmbeddingBackfillDto(),
                        ConvertToGuid(entity.Id),
                        EmbeddingEntityTypes.CommonPlant);
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Backfill failed for CommonPlant:{EntityId}", entity.Id);
                }
            }

            return processed;
        }

        private async Task<int> ProcessPlantInstanceBackfillBatchAsync(int skip, int take)
        {
            var items = await _unitOfWork.PlantInstanceRepository.GetEmbeddingBackfillBatchAsync(skip, take);

            var processed = 0;
            foreach (var entity in items)
            {
                try
                {
                    await _embeddingService.UpdateEmbeddingAsync(
                        entity.ToEmbeddingBackfillDto(),
                        ConvertToGuid(entity.Id),
                        EmbeddingEntityTypes.PlantInstance);
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Backfill failed for PlantInstance:{EntityId}", entity.Id);
                }
            }

            return processed;
        }

        private async Task<int> ProcessNurseryPlantComboBackfillBatchAsync(int skip, int take)
        {
            var items = await _unitOfWork.NurseryPlantComboRepository.GetEmbeddingBackfillBatchAsync(skip, take);

            var processed = 0;
            foreach (var entity in items)
            {
                try
                {
                    await _embeddingService.UpdateEmbeddingAsync(
                        entity.ToEmbeddingBackfillDto(),
                        ConvertToGuid(entity.Id),
                        EmbeddingEntityTypes.NurseryPlantCombo);
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Backfill failed for NurseryPlantCombo:{EntityId}", entity.Id);
                }
            }

            return processed;
        }

        private async Task<int> ProcessNurseryMaterialBackfillBatchAsync(int skip, int take)
        {
            var items = await _unitOfWork.NurseryMaterialRepository.GetEmbeddingBackfillBatchAsync(skip, take);

            var processed = 0;
            foreach (var entity in items)
            {
                try
                {
                    await _embeddingService.UpdateEmbeddingAsync(
                        entity.ToEmbeddingBackfillDto(),
                        ConvertToGuid(entity.Id),
                        EmbeddingEntityTypes.NurseryMaterial);
                    processed++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Backfill failed for NurseryMaterial:{EntityId}", entity.Id);
                }
            }

            return processed;
        }

        private static Guid ConvertToGuid(int id)
            => new Guid(id.ToString().PadLeft(32, '0'));

        private static int NormalizeBatchSize(int batchSize)
            => Math.Clamp(batchSize, 1, 100);

        private async Task EnrichPlantGuideAsync(CommonPlantEmbeddingDto dto)
        {
            if (dto.PlantId <= 0)
            {
                return;
            }

            var guide = await _unitOfWork.PlantGuideRepository.GetByPlantIdWithPlantAsync(dto.PlantId);
            if (guide == null)
            {
                return;
            }

            dto.GuideLightRequirement = guide.LightRequirement;
            dto.GuideLightRequirementName = GetLightRequirementName(guide.LightRequirement);
            dto.GuideWatering = guide.Watering;
            dto.GuideFertilizing = guide.Fertilizing;
            dto.GuidePruning = guide.Pruning;
            dto.GuideTemperature = guide.Temperature;
            dto.GuideHumidity = guide.Humidity;
            dto.GuideSoil = guide.Soil;
            dto.GuideCareNotes = guide.CareNotes;
        }

        private async Task EnrichPlantGuideAsync(PlantInstanceEmbeddingDto dto)
        {
            if (dto.PlantId <= 0)
            {
                return;
            }

            var guide = await _unitOfWork.PlantGuideRepository.GetByPlantIdWithPlantAsync(dto.PlantId);
            if (guide == null)
            {
                return;
            }

            dto.GuideLightRequirement = guide.LightRequirement;
            dto.GuideLightRequirementName = GetLightRequirementName(guide.LightRequirement);
            dto.GuideWatering = guide.Watering;
            dto.GuideFertilizing = guide.Fertilizing;
            dto.GuidePruning = guide.Pruning;
            dto.GuideTemperature = guide.Temperature;
            dto.GuideHumidity = guide.Humidity;
            dto.GuideSoil = guide.Soil;
            dto.GuideCareNotes = guide.CareNotes;
        }

        private static string? GetLightRequirementName(int? lightRequirement)
        {
            if (!lightRequirement.HasValue)
            {
                return null;
            }

            if (!Enum.IsDefined(typeof(LightRequirementEnum), lightRequirement.Value))
            {
                return null;
            }

            return ((LightRequirementEnum)lightRequirement.Value).ToString();
        }
    }
}
