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
        private readonly ILogger<EmbeddingService> _logger;

        public EmbeddingService(
            IUnitOfWork unitOfWork,
            IAzureOpenAIService azureOpenAIService,
            IEmbeddingTextSerializer textSerializer,
            ILogger<EmbeddingService> logger)
        {
            _unitOfWork = unitOfWork;
            _azureOpenAIService = azureOpenAIService;
            _textSerializer = textSerializer;
            _logger = logger;
        }

        public async Task<Embedding> CreateEmbeddingAsync<T>(T entity, Guid entityId, string entityType) where T : class
        {
            try
            {
                // Chuyển entity thành text để embedding (entity-specific)
                var content = SerializeEntityToText(entity, entityType);
                var metadata = ExtractMetadataFromEntity(entity, entityType);

                // Gọi Azure OpenAI để tạo embedding vector
                var embeddingArray = await _azureOpenAIService.GenerateEmbeddingAsync(content);

                if (embeddingArray == null || embeddingArray.Length == 0)
                {
                    throw new InvalidOperationException("Failed to generate embedding vector");
                }

                // Tạo entity Embedding
                var embedding = new Embedding
                {
                    Id = Guid.NewGuid(),
                    EntityType = entityType,
                    EntityId = entityId,
                    Content = content,
                    EmbeddingVector = new Vector(embeddingArray),
                    Metadata = metadata,
                    CreatedAt = DateTime.UtcNow
                };

                // Lưu vào database
                await _unitOfWork.EmbeddingRepository.CreateAsync(embedding);
                await _unitOfWork.SaveAsync();

                _logger.LogInformation($"Created embedding for {entityType}:{entityId}");

                return embedding;
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
                    => _textSerializer.ExtractMetadata(
                        cpDto.NurseryId,
                        cpDto.Price ?? cpDto.BasePrice,
                        cpDto.IsActive ? "Active" : "Inactive",
                        cpDto.CommonPlantId),

                EmbeddingEntityTypes.PlantInstance when entity is PlantInstanceEmbeddingDto piDto
                    => _textSerializer.ExtractMetadata(
                        piDto.NurseryId,
                        piDto.Price ?? piDto.SpecificPrice ?? piDto.BasePrice,
                        piDto.Status == 1 ? "Available" : "Unavailable",
                        piDto.PlantInstanceId),

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
    }
}
