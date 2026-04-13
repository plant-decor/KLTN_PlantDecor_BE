using Microsoft.Extensions.Logging;
using Pgvector;
using PlantDecor.BusinessLogicLayer.Constants;
using PlantDecor.BusinessLogicLayer.DTOs.Embedding;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.UnitOfWork;
using System.Text.Json;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class EmbeddingService : IEmbeddingService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAzureOpenAIService _azureOpenAIService;
        private readonly IEmbeddingTextSerializer _textSerializer;
        private readonly IEmbeddingTextPreprocessor _textPreprocessor;
        private readonly IEmbeddingChunker _chunker;
        private readonly ILogger<EmbeddingService> _logger;

        public EmbeddingService(
            IUnitOfWork unitOfWork,
            IAzureOpenAIService azureOpenAIService,
            IEmbeddingTextSerializer textSerializer,
            IEmbeddingTextPreprocessor textPreprocessor,
            IEmbeddingChunker chunker,
            ILogger<EmbeddingService> logger)
        {
            _unitOfWork = unitOfWork;
            _azureOpenAIService = azureOpenAIService;
            _textSerializer = textSerializer;
            _textPreprocessor = textPreprocessor;
            _chunker = chunker;
            _logger = logger;
        }

        public async Task<List<Embedding>> CreateEmbeddingAsync<T>(T entity, Guid entityId, string entityType) where T : class
        {
            try
            {
                if (entity == null)
                {
                    throw new ArgumentNullException(nameof(entity));
                }

                if (string.IsNullOrWhiteSpace(entityType))
                {
                    throw new ArgumentException("Entity type is required", nameof(entityType));
                }

                var rawContent = SerializeEntityToText(entity, entityType);
                var cleanedContent = _textPreprocessor.Preprocess(rawContent);
                var chunks = _chunker.Chunk(cleanedContent);
                if (chunks.Count == 0)
                {
                    throw new InvalidOperationException("No content available after preprocessing/chunking");
                }

                var metadata = ExtractMetadataFromEntity(entity, entityType);
                var chunkCount = chunks.Count;
                var now = DateTime.UtcNow;
                var embeddings = new List<Embedding>(chunkCount);
                var embeddingVectors = await _azureOpenAIService.GenerateEmbeddingsAsync(chunks);

                if (embeddingVectors.Count != chunkCount)
                {
                    throw new InvalidOperationException(
                        $"Embedding vector count mismatch: expected {chunkCount}, got {embeddingVectors.Count}");
                }

                for (var i = 0; i < chunkCount; i++)
                {
                    var chunkContent = chunks[i];
                    var embeddingArray = embeddingVectors[i];

                    if (embeddingArray == null || embeddingArray.Length == 0)
                    {
                        throw new InvalidOperationException($"Failed to generate embedding vector for chunk {i}");
                    }

                    embeddings.Add(new Embedding
                    {
                        Id = Guid.NewGuid(),
                        EntityType = entityType,
                        EntityId = entityId,
                        ChunkIndex = i,
                        ChunkCount = chunkCount,
                        Content = chunkContent,
                        EmbeddingVector = new Vector(embeddingArray),
                        Metadata = CloneMetadataWithChunkInfo(metadata, i, chunkCount),
                        CreatedAt = now
                    });
                }

                await _unitOfWork.EmbeddingRepository.AddRangeAsync(embeddings);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation($"Created {chunkCount} embedding chunk(s) for {entityType}:{entityId}");

                return embeddings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating embedding for {entityType}:{entityId}");
                throw;
            }
        }

        public async Task<List<Embedding>> SearchSimilarAsync(float[] queryVector, int limit = 10, string? entityType = null)
        {
            var vector = new Vector(queryVector);
            return await _unitOfWork.EmbeddingRepository.SearchSimilarAsync(vector, limit, entityType);
        }

        public async Task<Embedding?> GetByEntityAsync(string entityType, Guid entityId)
        {
            return await _unitOfWork.EmbeddingRepository.GetByEntityAsync(entityType, entityId);
        }

        public async Task<bool> DeleteByEntityAsync(string entityType, Guid entityId)
        {
            return await _unitOfWork.EmbeddingRepository.DeleteByEntityAsync(entityType, entityId);
        }

        public async Task UpdateEmbeddingAsync<T>(T entity, Guid entityId, string entityType) where T : class
        {
            try
            {
                // Xóa embedding cũ nếu có
                await DeleteByEntityAsync(entityType, entityId);

                // Tạo embedding mới
                await CreateEmbeddingAsync(entity, entityId, entityType);

                _logger.LogInformation($"Updated embedding for {entityType}:{entityId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating embedding for {entityType}:{entityId}");
                throw;
            }
        }

        private string SerializeEntityToText<T>(T entity, string entityType) where T : class
        {
            // Entity-specific serialization using EmbeddingTextSerializer
            return entityType switch
            {
                EmbeddingEntityTypes.CommonPlant when entity is CommonPlantEmbeddingDto cpDto
                    => _textSerializer.SerializeCommonPlant(cpDto),

                EmbeddingEntityTypes.PlantInstance when entity is PlantInstanceEmbeddingDto piDto
                    => _textSerializer.SerializePlantInstance(piDto),

                EmbeddingEntityTypes.NurseryPlantCombo when entity is NurseryPlantComboEmbeddingDto npcDto
                    => _textSerializer.SerializeNurseryPlantCombo(npcDto),

                EmbeddingEntityTypes.NurseryMaterial when entity is NurseryMaterialEmbeddingDto nmDto
                    => _textSerializer.SerializeNurseryMaterial(nmDto),

                // Fallback to JSON serialization for unknown types
                _ => JsonSerializer.Serialize(entity, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                })
            };
        }

        private Dictionary<string, object>? ExtractMetadataFromEntity<T>(T entity, string entityType) where T : class
        {
            return entityType switch
            {
                EmbeddingEntityTypes.CommonPlant when entity is CommonPlantEmbeddingDto cpDto
                    => AddPlantGuideMetadata(
                        _textSerializer.ExtractMetadata(
                            cpDto.NurseryId,
                            cpDto.Price ?? cpDto.BasePrice,
                            cpDto.IsActive ? "Active" : "Inactive",
                            cpDto.CommonPlantId),
                        cpDto.GuideLightRequirement,
                        cpDto.GuideLightRequirementName),

                EmbeddingEntityTypes.PlantInstance when entity is PlantInstanceEmbeddingDto piDto
                    => AddPlantGuideMetadata(
                        _textSerializer.ExtractMetadata(
                            piDto.NurseryId,
                            piDto.Price ?? piDto.SpecificPrice ?? piDto.BasePrice,
                            piDto.Status == 1 ? "Available" : "Unavailable",
                            piDto.PlantInstanceId),
                        piDto.GuideLightRequirement,
                        piDto.GuideLightRequirementName),

                EmbeddingEntityTypes.NurseryPlantCombo when entity is NurseryPlantComboEmbeddingDto npcDto
                    => _textSerializer.ExtractMetadata(
                        npcDto.NurseryId,
                        npcDto.Price ?? npcDto.ComboPrice,
                        npcDto.IsActive ? "Active" : "Inactive",
                        npcDto.NurseryPlantComboId),

                EmbeddingEntityTypes.NurseryMaterial when entity is NurseryMaterialEmbeddingDto nmDto
                    => _textSerializer.ExtractMetadata(
                        nmDto.NurseryId,
                        nmDto.Price ?? nmDto.BasePrice,
                        nmDto.IsActive ? "Active" : "Inactive",
                        nmDto.NurseryMaterialId),

                // Fallback for unknown types
                _ => new Dictionary<string, object> { ["EntityTypeName"] = typeof(T).Name }
            };
        }

        private static Dictionary<string, object> AddPlantGuideMetadata(
            Dictionary<string, object> metadata,
            int? guideLightRequirement,
            string? guideLightRequirementName)
        {
            if (guideLightRequirement.HasValue)
            {
                metadata["GuideLightRequirement"] = guideLightRequirement.Value;
            }

            if (!string.IsNullOrWhiteSpace(guideLightRequirementName))
            {
                metadata["GuideLightRequirementName"] = guideLightRequirementName;
            }

            return metadata;
        }

        private static Dictionary<string, object> CloneMetadataWithChunkInfo(
            Dictionary<string, object>? baseMetadata,
            int chunkIndex,
            int chunkCount)
        {
            var metadata = baseMetadata != null
                ? new Dictionary<string, object>(baseMetadata)
                : new Dictionary<string, object>();

            metadata["ChunkIndex"] = chunkIndex;
            metadata["ChunkCount"] = chunkCount;

            return metadata;
        }
    }
}
