using Microsoft.Extensions.Logging;
using Pgvector;
using PlantDecor.BusinessLogicLayer.Constants;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class AISearchService : IAISearchService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IAzureOpenAIService _azureOpenAIService;
        private readonly ILogger<AISearchService> _logger;

        public AISearchService(
            IUnitOfWork unitOfWork,
            IAzureOpenAIService azureOpenAIService,
            ILogger<AISearchService> logger)
        {
            _unitOfWork = unitOfWork;
            _azureOpenAIService = azureOpenAIService;
            _logger = logger;
        }

        public async Task<SemanticSearchResponseDto> SearchPurchasableAsync(
            string query,
            List<string>? entityTypes = null,
            int limit = 10,
            bool onlyPurchasable = true)
        {
            try
            {
                // 1. Generate embedding for query
                var queryVector = await _azureOpenAIService.GenerateEmbeddingAsync(query);
                if (queryVector == null || queryVector.Length == 0)
                {
                    _logger.LogWarning("Failed to generate embedding for query: {Query}", query);
                    return new SemanticSearchResponseDto
                    {
                        Results = new List<SearchResultItemDto>(),
                        TotalCount = 0,
                        Query = query
                    };
                }

                var vector = new Vector(queryVector);

                // 2. Search similar embeddings (get more for filtering)
                var searchLimit = onlyPurchasable ? limit * 3 : limit;
                var entityType = entityTypes?.FirstOrDefault();
                var embeddings = await _unitOfWork.EmbeddingRepository.SearchSimilarAsync(vector, searchLimit, entityType);

                // 3. Filter by purchasable status and enrich results
                var results = new List<SearchResultItemDto>();
                var maxDistance = embeddings.Any() ? embeddings.Max(e => GetDistance(e, vector)) : 1.0;

                foreach (var embedding in embeddings)
                {
                    var originalEntityId = ExtractOriginalEntityId(embedding.Metadata);
                    if (originalEntityId == 0) continue;

                    // Filter by entity types if specified
                    if (entityTypes != null && entityTypes.Any() && !entityTypes.Contains(embedding.EntityType))
                        continue;

                    // Check purchasable status
                    var isPurchasable = await CheckPurchasableAsync(embedding.EntityType, originalEntityId);
                    if (onlyPurchasable && !isPurchasable)
                        continue;

                    // Enrich result
                    var item = await EnrichSearchResultAsync(embedding, originalEntityId, isPurchasable, vector, maxDistance);
                    if (item != null)
                    {
                        results.Add(item);
                        if (results.Count >= limit)
                            break;
                    }
                }

                return new SemanticSearchResponseDto
                {
                    Results = results,
                    TotalCount = results.Count,
                    Query = query
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in semantic search for query: {Query}", query);
                throw;
            }
        }

        public async Task<RoomRecommendationResponseDto> GetRoomRecommendationsAsync(
            string roomDescription,
            string? fengShuiElement = null,
            decimal? maxBudget = null,
            int limit = 5,
            List<string>? preferredRooms = null,
            bool? petSafe = null,
            bool? childSafe = null)
        {
            try
            {
                // Build enhanced query for room recommendation
                var queryParts = new List<string> { roomDescription };

                if (!string.IsNullOrEmpty(fengShuiElement))
                    queryParts.Add($"Feng shui element {fengShuiElement}");

                if (preferredRooms != null && preferredRooms.Any())
                    queryParts.Add($"Suitable for {string.Join(", ", preferredRooms)}");

                if (petSafe == true)
                    queryParts.Add("Safe for pets");

                if (childSafe == true)
                    queryParts.Add("Safe for children");

                var query = string.Join(". ", queryParts);

                // Search for plants and combos
                var entityTypes = new List<string>
                {
                    EmbeddingEntityTypes.CommonPlant,
                    EmbeddingEntityTypes.PlantInstance,
                    EmbeddingEntityTypes.NurseryPlantCombo
                };

                var searchResult = await SearchPurchasableAsync(query, entityTypes, limit * 2, true);

                // Filter by budget and convert to recommendations
                var recommendations = new List<PlantRecommendationItemDto>();
                var rejectedBudget = 0;
                var rejectedPetSafe = 0;
                var rejectedChildSafe = 0;

                foreach (var item in searchResult.Results)
                {
                    if (maxBudget.HasValue && item.Price.HasValue && item.Price > maxBudget)
                    {
                        rejectedBudget++;
                        continue;
                    }

                    if (petSafe == true && item.PetSafe != true)
                    {
                        rejectedPetSafe++;
                        continue;
                    }

                    if (childSafe == true && item.ChildSafe != true)
                    {
                        rejectedChildSafe++;
                        continue;
                    }

                    var recommendation = new PlantRecommendationItemDto
                    {
                        EntityType = item.EntityType,
                        EntityId = item.EntityId,
                        Name = item.Name,
                        Description = item.Description,
                        Price = item.Price,
                        ImageUrl = item.ImageUrl,
                        FengShuiElement = item.FengShuiElement,
                        MatchScore = item.SimilarityScore,
                        NurseryId = item.NurseryId,
                        NurseryName = item.NurseryName,
                        ReasonForRecommendation = GenerateRecommendationReason(item, fengShuiElement, preferredRooms)
                    };

                    recommendations.Add(recommendation);

                    if (recommendations.Count >= limit)
                        break;
                }

                _logger.LogInformation(
                    "AI room recommendations filtered. Query='{Query}', SourceResults={SourceResults}, Returned={Returned}, RejectedBudget={RejectedBudget}, RejectedPetSafe={RejectedPetSafe}, RejectedChildSafe={RejectedChildSafe}, RequestedPetSafe={RequestedPetSafe}, RequestedChildSafe={RequestedChildSafe}",
                    query,
                    searchResult.Results.Count,
                    recommendations.Count,
                    rejectedBudget,
                    rejectedPetSafe,
                    rejectedChildSafe,
                    petSafe,
                    childSafe);

                return new RoomRecommendationResponseDto
                {
                    Recommendations = recommendations,
                    TotalCount = recommendations.Count,
                    RoomDescription = roomDescription,
                    FengShuiElement = fengShuiElement
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in room recommendations for: {RoomDescription}", roomDescription);
                throw;
            }
        }

        public async Task<List<PlantSuggestionResponseDto>> SuggestPlantsAsync(
            string? description = null,
            string? fengShuiElement = null,
            string? roomType = null,
            bool onlyPurchasable = true,
            int limit = 10,
            decimal? maxBudget = null)
        {
            try
            {
                // Build query from criteria
                var queryParts = new List<string>();

                if (!string.IsNullOrEmpty(description))
                    queryParts.Add(description);

                if (!string.IsNullOrEmpty(fengShuiElement))
                    queryParts.Add($"Feng shui element {fengShuiElement}");

                if (!string.IsNullOrEmpty(roomType))
                    queryParts.Add($"Suitable for {roomType}");

                if (!queryParts.Any())
                    queryParts.Add("Beautiful and easy-care plants");

                var query = string.Join(". ", queryParts);

                var plantEntityTypes = new List<string>
                {
                    EmbeddingEntityTypes.CommonPlant,
                    EmbeddingEntityTypes.PlantInstance
                };

                var searchResult = await SearchPurchasableAsync(query, plantEntityTypes, limit * 2, onlyPurchasable);

                var suggestions = searchResult.Results
                    .Where(r => !maxBudget.HasValue || !r.Price.HasValue || r.Price <= maxBudget)
                    .Take(limit)
                    .Select(r => new PlantSuggestionResponseDto
                    {
                        EntityType = r.EntityType,
                        EntityId = r.EntityId,
                        Name = r.Name,
                        Description = r.Description,
                        Price = r.Price,
                        ImageUrl = r.ImageUrl,
                        IsPurchasable = r.IsPurchasable,
                        RelevanceScore = r.SimilarityScore
                    })
                    .ToList();

                return suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in plant suggestions");
                throw;
            }
        }

        public async Task<bool> CheckPurchasableAsync(string entityType, int entityId)
        {
            try
            {
                switch (entityType)
                {
                    case EmbeddingEntityTypes.CommonPlant:
                        var commonPlant = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(entityId);
                        return commonPlant != null
                            && commonPlant.IsActive
                            && commonPlant.Quantity > 0;

                    case EmbeddingEntityTypes.PlantInstance:
                        var instance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(entityId);
                        return instance != null && instance.Status == 1; // 1 = Available

                    case EmbeddingEntityTypes.NurseryPlantCombo:
                        var combo = await _unitOfWork.NurseryPlantComboRepository.GetByIdAsync(entityId);
                        return combo != null && combo.IsActive && combo.Quantity > 0;

                    case EmbeddingEntityTypes.NurseryMaterial:
                        var material = await _unitOfWork.NurseryMaterialRepository.GetByIdWithDetailsAsync(entityId);
                        return material != null
                            && material.IsActive
                            && material.Quantity > 0
                            && (!material.ExpiredDate.HasValue || material.ExpiredDate.Value > DateOnly.FromDateTime(DateTime.Today));

                    default:
                        return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking purchasable status for {EntityType}:{EntityId}", entityType, entityId);
                return false;
            }
        }

        #region Private Helper Methods

        private int ExtractOriginalEntityId(Dictionary<string, object>? metadata)
        {
            if (metadata == null) return 0;

            if (metadata.TryGetValue("OriginalEntityId", out var idObj))
            {
                if (idObj is int intId) return intId;
                if (idObj is long longId) return (int)longId;
                if (idObj is string strId && int.TryParse(strId, out var parsedId)) return parsedId;
                if (idObj is System.Text.Json.JsonElement jsonElement)
                {
                    if (jsonElement.TryGetInt32(out var jsonId)) return jsonId;
                }
            }

            return 0;
        }

        private double GetDistance(DataAccessLayer.Entities.Embedding embedding, Vector queryVector)
        {
            // Since we don't have direct access to distance in the entity,
            // we calculate a normalized score based on position in results
            return 0.5; // Placeholder - actual distance calculated in repository
        }

        private async Task<SearchResultItemDto?> EnrichSearchResultAsync(
            DataAccessLayer.Entities.Embedding embedding,
            int originalEntityId,
            bool isPurchasable,
            Vector queryVector,
            double maxDistance)
        {
            var item = new SearchResultItemDto
            {
                EntityType = embedding.EntityType,
                EntityId = originalEntityId,
                IsPurchasable = isPurchasable,
                SimilarityScore = 0.8 // Default similarity score
            };

            // Extract metadata
            if (embedding.Metadata != null)
            {
                if (embedding.Metadata.TryGetValue("NurseryId", out var nurseryIdObj))
                {
                    item.NurseryId = ConvertToInt(nurseryIdObj);
                }
                if (embedding.Metadata.TryGetValue("Price", out var priceObj))
                {
                    item.Price = ConvertToDecimal(priceObj);
                }
            }

            // Enrich with entity-specific details
            switch (embedding.EntityType)
            {
                case EmbeddingEntityTypes.CommonPlant:
                    var commonPlant = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(originalEntityId);
                    if (commonPlant != null)
                    {
                        item.Name = commonPlant.Plant?.Name ?? "Unknown";
                        item.Description = commonPlant.Plant?.Description;
                        item.Price = commonPlant.Plant?.BasePrice;
                        item.NurseryId = commonPlant.NurseryId;
                        item.NurseryName = commonPlant.Nursery?.Name;
                        item.FengShuiElement = MapFengShuiElement(commonPlant.Plant?.FengShuiElement);
                        item.PetSafe = commonPlant.Plant?.PetSafe;
                        item.ChildSafe = commonPlant.Plant?.ChildSafe;
                        item.ImageUrl = commonPlant.Plant?.PlantImages?.FirstOrDefault()?.ImageUrl;
                    }
                    break;

                case EmbeddingEntityTypes.PlantInstance:
                    var instance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(originalEntityId);
                    if (instance != null)
                    {
                        item.Name = instance.Plant?.Name ?? "Unknown";
                        item.Description = instance.Description ?? instance.Plant?.Description;
                        item.Price = instance.SpecificPrice ?? instance.Plant?.BasePrice;
                        item.NurseryId = instance.CurrentNurseryId ?? 0;
                        item.NurseryName = instance.CurrentNursery?.Name;
                        item.FengShuiElement = MapFengShuiElement(instance.Plant?.FengShuiElement);
                        item.PetSafe = instance.Plant?.PetSafe;
                        item.ChildSafe = instance.Plant?.ChildSafe;
                        item.ImageUrl = instance.PlantImages?.FirstOrDefault()?.ImageUrl ?? instance.Plant?.PlantImages?.FirstOrDefault()?.ImageUrl;
                    }
                    break;

                case EmbeddingEntityTypes.NurseryPlantCombo:
                    var combo = await _unitOfWork.NurseryPlantComboRepository.GetByIdAsync(originalEntityId);
                    if (combo != null)
                    {
                        item.Name = combo.PlantCombo?.ComboName ?? "Unknown";
                        item.Description = combo.PlantCombo?.Description;
                        item.Price = combo.PlantCombo?.ComboPrice;
                        item.NurseryId = combo.NurseryId;
                        item.NurseryName = combo.Nursery?.Name;
                        item.FengShuiElement = MapFengShuiElement(combo.PlantCombo?.FengShuiElement);
                        item.PetSafe = combo.PlantCombo?.PetSafe;
                        item.ChildSafe = combo.PlantCombo?.ChildSafe;
                        item.ImageUrl = combo.PlantCombo?.PlantComboImages?.FirstOrDefault()?.ImageUrl;
                    }
                    break;

                case EmbeddingEntityTypes.NurseryMaterial:
                    var material = await _unitOfWork.NurseryMaterialRepository.GetByIdWithDetailsAsync(originalEntityId);
                    if (material != null)
                    {
                        item.Name = material.Material?.Name ?? "Unknown";
                        item.Description = material.Material?.Description;
                        item.Price = material.Material?.BasePrice;
                        item.NurseryId = material.NurseryId;
                        item.NurseryName = material.Nursery?.Name;
                        item.ImageUrl = material.Material?.MaterialImages?.FirstOrDefault()?.ImageUrl;
                    }
                    break;
            }

            return item;
        }

        private string GenerateRecommendationReason(SearchResultItemDto item, string? fengShuiElement, List<string>? preferredRooms)
        {
            var reasons = new List<string>();

            if (!string.IsNullOrEmpty(fengShuiElement) && item.FengShuiElement == fengShuiElement)
            {
                reasons.Add($"Compatible with feng shui element {fengShuiElement}");
            }

            if (item.SimilarityScore > 0.8)
            {
                reasons.Add("Highly relevant to your description");
            }

            if (item.IsPurchasable)
            {
                reasons.Add("In stock and available for purchase");
            }

            return reasons.Any() ? string.Join(". ", reasons) : "Matches your preferences";
        }

        private int ConvertToInt(object? obj)
        {
            if (obj == null) return 0;
            if (obj is int i) return i;
            if (obj is long l) return (int)l;
            if (obj is string s && int.TryParse(s, out var result)) return result;
            if (obj is System.Text.Json.JsonElement je && je.TryGetInt32(out var jResult)) return jResult;
            return 0;
        }

        private decimal ConvertToDecimal(object? obj)
        {
            if (obj == null) return 0;
            if (obj is decimal d) return d;
            if (obj is double dbl) return (decimal)dbl;
            if (obj is float f) return (decimal)f;
            if (obj is int i) return i;
            if (obj is long l) return l;
            if (obj is string s && decimal.TryParse(s, out var result)) return result;
            if (obj is System.Text.Json.JsonElement je)
            {
                if (je.TryGetDecimal(out var jResult)) return jResult;
                if (je.TryGetDouble(out var jDbl)) return (decimal)jDbl;
            }
            return 0;
        }

        private static string? MapFengShuiElement(int? fengShuiElement)
        {
            if (!fengShuiElement.HasValue)
            {
                return null;
            }

            return Enum.IsDefined(typeof(FengShuiElementTypeEnum), fengShuiElement.Value)
                ? ((FengShuiElementTypeEnum)fengShuiElement.Value).ToString()
                : null;
        }

        #endregion
    }
}
