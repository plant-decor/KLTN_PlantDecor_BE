using Microsoft.Extensions.Logging;
using Pgvector;
using PlantDecor.BusinessLogicLayer.Constants;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.UnitOfWork;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class RoomDesignService : IRoomDesignService
    {
        private const int DEFAULT_RECOMMENDATION_LIMIT = 3;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IAzureOpenAIService _azureOpenAIService;
        private readonly IAISearchService _aiSearchService;
        private readonly ILogger<RoomDesignService> _logger;

        private const string ROOM_ANALYSIS_PROMPT = @"
Bạn là chuyên gia thiết kế nội thất và cây cảnh. Hãy phân tích ảnh căn phòng này và trả về JSON với cấu trúc sau:
{
    ""roomType"": ""living_room|bedroom|office|kitchen|bathroom|balcony|garden|other"",
    ""roomSize"": ""small|medium|large"",
    ""lightingCondition"": ""low|medium|high|natural"",
    ""interiorStyle"": ""modern|minimalist|tropical|classic|industrial|scandinavian|bohemian|other"",
    ""availableSpace"": ""floor|table|shelf|hanging|windowsill|corner"",
    ""colorPalette"": [""color1"", ""color2"", ""color3""],
    ""placementSuggestions"": [""suggestion1"", ""suggestion2""],
    ""summary"": ""Tóm tắt ngắn gọn về căn phòng và loại cây phù hợp""
}

Lưu ý:
- Đánh giá ánh sáng dựa trên cửa sổ, đèn, góc chụp
- Xác định không gian trống có thể đặt cây
- Đề xuất vị trí đặt cây phù hợp với phong thủy và thẩm mỹ
- Trả lời bằng tiếng Việt cho summary và placementSuggestions
";

        private const string RECOMMENDATION_PROMPT_TEMPLATE = @"
Dựa vào phân tích căn phòng:
- Loại phòng: {0}
- Kích thước: {1}
- Ánh sáng: {2}
- Phong cách: {3}
- Vị trí đặt cây: {4}

Và danh sách các cây có sẵn trong hệ thống:
{5}

Hãy chọn ra {6} cây phù hợp nhất và trả về JSON array với format:
[
    {{
        ""entityType"": ""CommonPlant|PlantInstance"",
        ""entityId"": 123,
        ""reasonForRecommendation"": ""Lý do cây này phù hợp với căn phòng"",
        ""suggestedPlacement"": ""Viết 1 câu văn tiếng Việt tự nhiên chỉ ra vị trí đặt cây cụ thể trong căn phòng này (VD: Đặt trên bàn làm việc, hoặc góc phòng cạnh sofa)"",
        ""matchScore"": 0.95
    }}
]

Tiêu chí đánh giá:
1. Phù hợp với điều kiện ánh sáng của phòng
2. Kích thước cây phù hợp với không gian
3. Phong cách cây phù hợp với nội thất
4. Dễ chăm sóc cho môi trường trong nhà
5. {7}
6. Ưu tiên đa dạng loài, tránh lặp lại cùng một tên cây nếu còn lựa chọn phù hợp khác

Lưu ý quan trọng:
- suggestedPlacement PHẢI là câu tiếng Việt đầy đủ, mô tả vị trí cụ thể phù hợp với từng cây
- Tham khảo gợi ý bố trí từ phân tích ảnh nhưng điều chỉnh phù hợp với đặc tính từng cây

Chỉ trả về JSON array, không có text khác.
";

        public RoomDesignService(
            IUnitOfWork unitOfWork,
            IAzureOpenAIService azureOpenAIService,
            IAISearchService aiSearchService,
            ILogger<RoomDesignService> logger)
        {
            _unitOfWork = unitOfWork;
            _azureOpenAIService = azureOpenAIService;
            _aiSearchService = aiSearchService;
            _logger = logger;
        }

        public async Task<RoomDesignResponseDto> AnalyzeAndRecommendAsync(RoomDesignRequestDto request)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var fengShuiFilter = request.FengShuiElement?.ToString();
                NormalizeRequestFilters(request);

                _logger.LogInformation(
                    "Room design filters normalized. MaxBudget={MaxBudget}, PreferredNurseryIds={PreferredNurseryIds}, FengShuiElement={FengShuiElement}, PetSafe={PetSafe}, ChildSafe={ChildSafe}",
                    request.MaxBudget,
                    request.PreferredNurseryIds == null ? "null" : string.Join(",", request.PreferredNurseryIds),
                    fengShuiFilter,
                    request.PetSafe,
                    request.ChildSafe);

                // Step 1: Analyze room image using Vision API
                _logger.LogInformation("Starting room analysis...");
                var roomAnalysis = await AnalyzeRoomAsync(request.RoomImageBase64);

                // Step 2: Build search query based on room analysis
                var searchQuery = BuildSearchQuery(roomAnalysis, request);
                _logger.LogInformation("Search query: {Query}", searchQuery);

                // Step 3: Search for plants in database using embeddings
                var candidatePlants = await SearchCandidatePlantsAsync(searchQuery, request);

                // Step 4: If we have enough candidates, use AI to re-rank and explain
                var recommendations = await RankAndExplainRecommendationsAsync(
                    roomAnalysis,
                    candidatePlants,
                    request,
                    fengShuiFilter);

                // Overwrite AI vision summary with a grounded summary based on DB-backed recommendations.
                roomAnalysis.Summary = BuildGroundedSummary(roomAnalysis, recommendations);

                stopwatch.Stop();

                return new RoomDesignResponseDto
                {
                    RoomAnalysis = roomAnalysis,
                    Recommendations = recommendations,
                    TotalCount = recommendations.Count,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in room design analysis");
                throw;
            }
        }

        public async Task<RoomAnalysisDto> AnalyzeRoomAsync(string imageBase64)
        {
            try
            {
                var response = await _azureOpenAIService.AnalyzeImageAsync(imageBase64, ROOM_ANALYSIS_PROMPT);

                if (string.IsNullOrEmpty(response))
                {
                    throw new InvalidOperationException("Failed to analyze room image");
                }

                // Parse JSON response
                var jsonStart = response.IndexOf('{');
                var jsonEnd = response.LastIndexOf('}');
                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    response = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                }

                var analysisResult = JsonSerializer.Deserialize<RoomAnalysisJsonDto>(response, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (analysisResult == null)
                {
                    throw new InvalidOperationException("Failed to parse room analysis response");
                }

                return analysisResult.ToRoomAnalysisDto(MapRoomType);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error parsing room analysis JSON");
                // Return default analysis if parsing fails
                return new RoomAnalysisDto
                {
                    RoomType = "Phòng khách",
                    RoomSize = "medium",
                    LightingCondition = "medium",
                    InteriorStyle = "modern",
                    AvailableSpace = "floor",
                    ColorPalette = new List<string>(),
                    Summary = "Không thể phân tích chi tiết, đề xuất cây phù hợp chung"
                };
            }
        }

        private string BuildSearchQuery(RoomAnalysisDto analysis, RoomDesignRequestDto request)
        {
            var queryParts = new List<string>();
            var fengShuiFilter = request.FengShuiElement?.ToString();

            // Room-based criteria
            queryParts.Add($"Cây phù hợp cho {analysis.RoomType}");

            // Lighting
            var lightingQuery = analysis.LightingCondition switch
            {
                "low" => "cây chịu bóng, ít ánh sáng",
                "high" or "natural" => "cây ưa sáng, cần nhiều ánh sáng",
                _ => "cây bán bóng"
            };
            queryParts.Add(lightingQuery);

            // Size
            var sizeQuery = analysis.RoomSize switch
            {
                "small" => "cây nhỏ gọn, cây để bàn",
                "large" => "cây lớn, cây trang trí",
                _ => "cây vừa"
            };
            queryParts.Add(sizeQuery);

            // Style
            var styleQuery = analysis.InteriorStyle switch
            {
                "minimalist" or "scandinavian" => "cây đơn giản, thanh lịch",
                "tropical" => "cây nhiệt đới, lá lớn",
                "industrial" => "cây công nghiệp, xương rồng, sen đá",
                "bohemian" => "cây dây leo, cây treo",
                _ => "cây cảnh đẹp"
            };
            queryParts.Add(styleQuery);

            // Feng shui element
            if (!string.IsNullOrEmpty(fengShuiFilter))
            {
                queryParts.Add($"phong thủy mệnh {fengShuiFilter}");
            }

            // Safety
            if (request.PetSafe == true)
            {
                queryParts.Add("an toàn cho thú cưng, không độc");
            }
            if (request.ChildSafe == true)
            {
                queryParts.Add("an toàn cho trẻ em");
            }

            if (!string.IsNullOrWhiteSpace(analysis.Summary))
            {
                queryParts.Add($"tóm tắt phòng: {analysis.Summary}");
            }

            return string.Join(", ", queryParts);
        }

        private async Task<List<PlantRecommendationDto>> SearchCandidatePlantsAsync(
            string searchQuery,
            RoomDesignRequestDto request)
        {
            // Generate embedding for search query
            var queryVector = await _azureOpenAIService.GenerateEmbeddingAsync(searchQuery);
            if (queryVector == null || queryVector.Length == 0)
            {
                _logger.LogWarning("Failed to generate embedding for search query");
                return new List<PlantRecommendationDto>();
            }

            var vector = new Vector(queryVector);
            var limit = DEFAULT_RECOMMENDATION_LIMIT;
            var fengShuiFilter = request.FengShuiElement?.ToString();

            // Search for similar plants - get more than needed for filtering
            var searchLimit = (limit + 5) * 3;
            var roomDesignEntityTypes = new[]
            {
                EmbeddingEntityTypes.CommonPlant,
                EmbeddingEntityTypes.PlantInstance
            };

            // Fetch a wider candidate set per type so named hints from room analysis can surface.
            var perTypeLimit = Math.Max(40, searchLimit * 2);
            var embeddingsByType = new Dictionary<string, List<EmbeddingSearchItemDto>>(StringComparer.OrdinalIgnoreCase);

            foreach (var entityType in roomDesignEntityTypes)
            {
                var rawEmbeddings = await _unitOfWork.EmbeddingRepository
                    .SearchSimilarAsync(vector, perTypeLimit, entityType);

                embeddingsByType[entityType] = rawEmbeddings
                    .Select(RoomDesignMapper.ToEmbeddingSearchItem)
                    .ToList();
            }

            // Interleave per-type result lists so one type doesn't dominate candidate selection.
            var embeddings = new List<EmbeddingSearchItemDto>();
            for (var index = 0; index < perTypeLimit; index++)
            {
                var added = false;
                foreach (var entityType in roomDesignEntityTypes)
                {
                    var typedEmbeddings = embeddingsByType[entityType];
                    if (index < typedEmbeddings.Count)
                    {
                        embeddings.Add(typedEmbeddings[index]);
                        added = true;
                    }
                }

                if (!added)
                {
                    break;
                }
            }

            var candidates = new List<PlantRecommendationDto>();
            var seenEntities = new HashSet<string>();
            var seenPlantNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rejectedMissingOriginalId = 0;
            var rejectedDuplicate = 0;
            var rejectedDuplicatePlantName = 0;
            var rejectedNotPurchasable = 0;
            var rejectedNullCandidate = 0;
            var rejectedBudget = 0;
            var rejectedPreferredNursery = 0;
            var rejectedFengShui = 0;

            foreach (var embedding in embeddings)
            {
                // Extract original entity ID from metadata
                var originalEntityId = ExtractOriginalEntityId(embedding.Metadata);
                if (originalEntityId == 0)
                {
                    rejectedMissingOriginalId++;
                    continue;
                }

                var dedupeKey = $"{embedding.EntityType}:{originalEntityId}";
                if (!seenEntities.Add(dedupeKey))
                {
                    rejectedDuplicate++;
                    continue;
                }

                // Check if purchasable
                var isPurchasable = await _aiSearchService.CheckPurchasableAsync(embedding.EntityType, originalEntityId);
                if (!isPurchasable)
                {
                    rejectedNotPurchasable++;
                    continue;
                }

                // Get entity details
                var candidate = await EnrichCandidateAsync(embedding.EntityType, originalEntityId);
                if (candidate == null)
                {
                    rejectedNullCandidate++;
                    continue;
                }

                // Apply filters
                if (request.MaxBudget.HasValue && candidate.Price > request.MaxBudget)
                {
                    rejectedBudget++;
                    continue;
                }

                if (request.PreferredNurseryIds?.Any() == true &&
                    !request.PreferredNurseryIds.Contains(candidate.NurseryId))
                {
                    rejectedPreferredNursery++;
                    continue;
                }

                candidate.IsPurchasable = true;

                // When user specifies feng shui element, enforce strict matching.
                if (!string.IsNullOrWhiteSpace(fengShuiFilter) &&
                    !IsFengShuiMatch(candidate.FengShuiElement, fengShuiFilter))
                {
                    rejectedFengShui++;
                    continue;
                }

                // Keep one representative per plant name across nurseries and sources.
                var plantNameKey = NormalizePlantName(candidate.Name);
                if (!string.IsNullOrEmpty(plantNameKey) && !seenPlantNames.Add(plantNameKey))
                {
                    rejectedDuplicatePlantName++;
                    continue;
                }

                candidates.Add(candidate);

                if (candidates.Count >= searchLimit)
                    break;
            }

            _logger.LogInformation(
                "Room design candidate search completed. Query='{Query}', Embeddings={Embeddings}, Candidates={Candidates}, RejectedMissingOriginalId={RejectedMissingOriginalId}, RejectedDuplicate={RejectedDuplicate}, RejectedDuplicatePlantName={RejectedDuplicatePlantName}, RejectedNotPurchasable={RejectedNotPurchasable}, RejectedNullCandidate={RejectedNullCandidate}, RejectedBudget={RejectedBudget}, RejectedPreferredNursery={RejectedPreferredNursery}, RejectedFengShui={RejectedFengShui}",
                searchQuery,
                embeddings.Count,
                candidates.Count,
                rejectedMissingOriginalId,
                rejectedDuplicate,
                rejectedDuplicatePlantName,
                rejectedNotPurchasable,
                rejectedNullCandidate,
                rejectedBudget,
                rejectedPreferredNursery,
                rejectedFengShui);

            return candidates;
        }

        private async Task<List<PlantRecommendationDto>> RankAndExplainRecommendationsAsync(
            RoomAnalysisDto roomAnalysis,
            List<PlantRecommendationDto> candidates,
            RoomDesignRequestDto request,
            string? fengShuiFilter)
        {
            var limit = DEFAULT_RECOMMENDATION_LIMIT;

            if (!candidates.Any())
            {
                return new List<PlantRecommendationDto>();
            }

            var hintPrioritizedCandidates = PrioritizeCandidatesByRoomHints(candidates, roomAnalysis);

            // Use AI to rank and explain top candidates
            try
            {
                var candidatesJson = JsonSerializer.Serialize(hintPrioritizedCandidates.Take(20).Select(c => new
                {
                    c.EntityType,
                    c.EntityId,
                    c.Name,
                    c.Description,
                    c.Price,
                    c.FengShuiElement,
                    c.CareDifficulty
                }), new JsonSerializerOptions { WriteIndented = false });

                var additionalCriteria = !string.IsNullOrEmpty(fengShuiFilter)
                    ? $"Ưu tiên cây phù hợp mệnh {fengShuiFilter}"
                    : "Cân nhắc phong thủy nếu có thông tin";

                var prompt = string.Format(
                    RECOMMENDATION_PROMPT_TEMPLATE,
                    roomAnalysis.RoomType,
                    roomAnalysis.RoomSize,
                    roomAnalysis.LightingCondition,
                    roomAnalysis.InteriorStyle,
                    roomAnalysis.AvailableSpace,
                    candidatesJson,
                    limit,
                    additionalCriteria);

                var response = await _azureOpenAIService.GenerateChatCompletionAsync(
                    "Bạn là chuyên gia về cây cảnh và thiết kế nội thất. Trả lời bằng JSON array.",
                    prompt,
                    0.3f);

                if (!string.IsNullOrEmpty(response))
                {
                    // Parse JSON array
                    var jsonStart = response.IndexOf('[');
                    var jsonEnd = response.LastIndexOf(']');
                    if (jsonStart >= 0 && jsonEnd > jsonStart)
                    {
                        response = response.Substring(jsonStart, jsonEnd - jsonStart + 1);
                    }

                    var rankings = JsonSerializer.Deserialize<List<RankingResultDto>>(response, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (rankings != null)
                    {
                        var result = new List<PlantRecommendationDto>();
                        foreach (var ranking in rankings.Take(limit))
                        {
                            var candidate = !string.IsNullOrWhiteSpace(ranking.EntityType)
                                ? hintPrioritizedCandidates.FirstOrDefault(c =>
                                    c.EntityId == ranking.EntityId &&
                                    c.EntityType.Equals(ranking.EntityType, StringComparison.OrdinalIgnoreCase))
                                : hintPrioritizedCandidates.FirstOrDefault(c => c.EntityId == ranking.EntityId);

                            if (candidate != null)
                            {
                                candidate.ReasonForRecommendation = ranking.ReasonForRecommendation
                                    ?? GenerateBasicReason(candidate, roomAnalysis);
                                candidate.SuggestedPlacement = BuildSuggestedPlacementText(
                                    ranking.SuggestedPlacement,
                                    roomAnalysis);
                                candidate.MatchScore = ranking.MatchScore > 0 ? ranking.MatchScore : 0.8;
                                result.Add(candidate);
                            }
                        }
                        return FinalizeRecommendations(
                            result,
                            hintPrioritizedCandidates,
                            roomAnalysis,
                            fengShuiFilter,
                            limit);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to use AI for ranking, falling back to basic ranking");
            }

            // Fallback: return top candidates with basic reasoning
            var fallbackPlacement = BuildFallbackPlacementText(roomAnalysis);
            var fallbackPrioritized = hintPrioritizedCandidates.Select(c =>
            {
                c.ReasonForRecommendation = GenerateBasicReason(c, roomAnalysis);
                c.SuggestedPlacement = fallbackPlacement;
                c.MatchScore = 0.75;
                return c;
            }).ToList();

            return FinalizeRecommendations(
                fallbackPrioritized,
                hintPrioritizedCandidates,
                roomAnalysis,
                fengShuiFilter,
                limit);
        }

        private static string BuildFallbackPlacementText(RoomAnalysisDto roomAnalysis)
        {
            var availableSpaceSuggestions = MapAvailableSpaceToVietnamese(roomAnalysis.AvailableSpace);
            if (availableSpaceSuggestions.Count == 0)
            {
                return "Đặt ở vị trí có ánh sáng và không gian phù hợp theo đặc tính của cây.";
            }

            var merged = string.Join(" hoặc ", availableSpaceSuggestions);
            return $"Vị trí tham khảo: {merged}.";
        }

        private static List<string> MapAvailableSpaceToVietnamese(string? availableSpace)
        {
            if (string.IsNullOrWhiteSpace(availableSpace))
            {
                return new List<string>();
            }

            return availableSpace
                .Split(new[] { '|', ',', ';', '/' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(token => token switch
                {
                    "floor" => "khu vực sàn gần góc thoáng",
                    "table" => "trên bàn hoặc mặt kệ thấp",
                    "corner" => "góc phòng",
                    "shelf" => "trên kệ",
                    "hanging" => "vị trí treo gần nguồn sáng tán xạ",
                    "windowsill" => "khu vực gần bệ cửa sổ",
                    _ => token
                })
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        private static string BuildSuggestedPlacementText(string? aiSuggestedPlacement, RoomAnalysisDto roomAnalysis)
        {
            var fallback = BuildFallbackPlacementText(roomAnalysis);
            if (string.IsNullOrWhiteSpace(aiSuggestedPlacement))
            {
                return fallback;
            }

            var raw = aiSuggestedPlacement.Trim();
            var looksLikeTokenFormat = raw.Contains('|') || raw.Contains('/');
            if (looksLikeTokenFormat)
            {
                var tokens = raw
                    .Split(new[] { '|', '/', ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(t => t.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3)
                    .ToList();

                if (tokens.Count == 0)
                {
                    return fallback;
                }

                var mapped = tokens.Select(MapPlacementTokenToVietnamese).ToList();
                return $"Đặt cây ở {string.Join(" hoặc ", mapped)} để hài hòa không gian và thuận tiện chăm sóc.";
            }

            if (!raw.EndsWith('.') && !raw.EndsWith('!') && !raw.EndsWith('?'))
            {
                raw += ".";
            }

            return raw;
        }

        private static string MapPlacementTokenToVietnamese(string token)
        {
            return token.Trim().ToLowerInvariant() switch
            {
                "floor" => "khu vực sàn gần góc thoáng",
                "table" => "trên bàn hoặc mặt kệ thấp",
                "corner" => "góc phòng",
                "shelf" => "trên kệ",
                "hanging" => "vị trí treo gần nguồn sáng tán xạ",
                "windowsill" => "khu vực gần bệ cửa sổ",
                _ => token
            };
        }

        private static string BuildGroundedSummary(
            RoomAnalysisDto roomAnalysis,
            IReadOnlyCollection<PlantRecommendationDto> recommendations)
        {
            var roomPart = $"{roomAnalysis.RoomType} phong cách {roomAnalysis.InteriorStyle}, ánh sáng {MapLightingConditionToVietnamese(roomAnalysis.LightingCondition)}.";

            if (recommendations.Count == 0)
            {
                return $"{roomPart} Hiện chưa có cây phù hợp theo các bộ lọc đã chọn trong dữ liệu hệ thống.";
            }

            var topNames = recommendations
                .Select(r => r.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(3)
                .ToList();

            if (topNames.Count == 0)
            {
                return $"{roomPart} Đề xuất được tạo từ dữ liệu cây hiện có trong hệ thống.";
            }

            var namesPart = string.Join(", ", topNames);
            return $"{roomPart} Dựa trên dữ liệu cây trong hệ thống, các lựa chọn phù hợp gồm: {namesPart}.";
        }

        private static string MapLightingConditionToVietnamese(string? lightingCondition)
        {
            return lightingCondition?.Trim().ToLowerInvariant() switch
            {
                "low" => "thấp",
                "medium" => "trung bình",
                "high" => "cao",
                "natural" => "tự nhiên",
                _ => lightingCondition ?? "không xác định"
            };
        }

        private string GenerateBasicReason(PlantRecommendationDto plant, RoomAnalysisDto room)
        {
            var reasons = new List<string>();

            reasons.Add($"Phù hợp với {room.RoomType}");

            if (!string.IsNullOrEmpty(plant.FengShuiElement))
            {
                reasons.Add($"mệnh {plant.FengShuiElement}");
            }

            reasons.Add("còn hàng, có thể mua ngay");

            return string.Join(", ", reasons);
        }

        private async Task<PlantRecommendationDto?> EnrichCandidateAsync(string entityType, int entityId)
        {
            try
            {
                switch (entityType)
                {
                    case EmbeddingEntityTypes.CommonPlant:
                        var commonPlant = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(entityId);
                        if (commonPlant?.Plant == null) return null;
                        var commonPlantImageUrl = await _unitOfWork.CommonPlantRepository.GetPrimaryImageUrlAsync(entityId);
                        return new PlantRecommendationDto
                        {
                            EntityType = entityType,
                            EntityId = entityId,
                            ProductId = commonPlant.Id,
                            Name = commonPlant.Plant.Name,
                            Description = commonPlant.Plant.Description,
                            Price = commonPlant.Plant.BasePrice,
                            ImageUrl = commonPlantImageUrl,
                            FengShuiElement = commonPlant.Plant.FengShuiElement,
                            CareDifficulty = MapCareDifficulty(commonPlant.Plant.CareLevelType),
                            NurseryId = commonPlant.NurseryId,
                            NurseryName = commonPlant.Nursery?.Name
                        };

                    case EmbeddingEntityTypes.PlantInstance:
                        var instance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(entityId);
                        if (instance?.Plant == null) return null;
                        var instanceImageUrl = await _unitOfWork.PlantInstanceRepository.GetPrimaryImageUrlAsync(entityId);
                        return new PlantRecommendationDto
                        {
                            EntityType = entityType,
                            EntityId = entityId,
                            ProductId = instance.Id,
                            Name = instance.Plant.Name,
                            Description = instance.Description ?? instance.Plant.Description,
                            Price = instance.SpecificPrice ?? instance.Plant.BasePrice,
                            ImageUrl = instanceImageUrl,
                            FengShuiElement = instance.Plant.FengShuiElement,
                            CareDifficulty = MapCareDifficulty(instance.Plant.CareLevelType),
                            NurseryId = instance.CurrentNurseryId ?? 0,
                            NurseryName = instance.CurrentNursery?.Name
                        };

                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enriching candidate {EntityType}:{EntityId}", entityType, entityId);
                return null;
            }
        }

        private int ExtractOriginalEntityId(Dictionary<string, object>? metadata)
        {
            if (metadata == null) return 0;

            if (metadata.TryGetValue("OriginalEntityId", out var idObj))
            {
                if (idObj is int intId) return intId;
                if (idObj is long longId) return (int)longId;
                if (idObj is string strId && int.TryParse(strId, out var parsedId)) return parsedId;
                if (idObj is JsonElement jsonElement && jsonElement.TryGetInt32(out var jsonId)) return jsonId;
            }

            return 0;
        }

        private static string MapCareDifficulty(int? careLevelType)
        {
            return careLevelType switch
            {
                1 => "Easy",
                2 => "Medium",
                3 => "Hard",
                4 => "Expert",
                _ => "Unknown"
            };
        }

        private static bool IsFengShuiMatch(string? candidateElement, string? requestedElement)
        {
            if (string.IsNullOrWhiteSpace(requestedElement))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(candidateElement))
            {
                return false;
            }

            var normalizedCandidate = NormalizeFengShuiElement(candidateElement);
            var normalizedRequested = NormalizeFengShuiElement(requestedElement);

            return IsFengShuiCompatibleNormalized(normalizedCandidate, normalizedRequested);
        }

        private static string NormalizeFengShuiElement(string value)
        {
            var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);

            foreach (var ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(ch);
                }
            }

            var normalized = sb
                .ToString()
                .Replace("đ", "d", StringComparison.OrdinalIgnoreCase)
                .Replace(" ", string.Empty, StringComparison.Ordinal)
                .ToUpperInvariant();

            return normalized switch
            {
                "KIM" => "KIM",
                "MOC" => "MOC",
                "THUY" => "THUY",
                "HOA" => "HOA",
                "THO" => "THO",
                _ => normalized
            };
        }

        private static List<PlantRecommendationDto> BuildDiverseRecommendations(
            IEnumerable<PlantRecommendationDto> prioritized,
            IEnumerable<PlantRecommendationDto> fallbackPool,
            int limit)
        {
            if (limit <= 0)
            {
                return new List<PlantRecommendationDto>();
            }

            var selected = new List<PlantRecommendationDto>(limit);
            var seenEntities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenPlantNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void TryAdd(PlantRecommendationDto candidate)
            {
                if (selected.Count >= limit)
                {
                    return;
                }

                var entityKey = $"{candidate.EntityType}:{candidate.EntityId}";
                if (seenEntities.Contains(entityKey))
                {
                    return;
                }

                var nameKey = NormalizePlantName(candidate.Name);
                if (!string.IsNullOrEmpty(nameKey) && seenPlantNames.Contains(nameKey))
                {
                    return;
                }

                seenEntities.Add(entityKey);

                if (!string.IsNullOrEmpty(nameKey))
                {
                    seenPlantNames.Add(nameKey);
                }

                selected.Add(candidate);
            }

            foreach (var candidate in prioritized)
            {
                TryAdd(candidate);
            }

            if (selected.Count < limit)
            {
                foreach (var candidate in fallbackPool)
                {
                    TryAdd(candidate);
                }
            }

            return selected;
        }

        private static List<PlantRecommendationDto> FinalizeRecommendations(
            IEnumerable<PlantRecommendationDto> prioritized,
            IEnumerable<PlantRecommendationDto> fallbackPool,
            RoomAnalysisDto roomAnalysis,
            string? requestedFengShuiElement,
            int limit)
        {
            var selected = BuildDiverseRecommendations(prioritized, fallbackPool, limit);
            if (selected.Count == 0)
            {
                return selected;
            }

            // Keep AI-ranked results stable when we already have enough items.
            // Coverage/alignment replacement is only used to recover underfilled sets.
            if (selected.Count < limit)
            {
                var targetElement = ResolveTargetFengShuiElement(requestedFengShuiElement);
                selected = EnsureHintCoverage(selected, fallbackPool, roomAnalysis, targetElement, limit);
                selected = AlignToTargetFengShuiElement(selected, fallbackPool, targetElement, limit);
            }

            selected = EnsureUniquePlantNames(selected, fallbackPool, limit);

            return selected.Take(limit).ToList();
        }

        private static List<PlantRecommendationDto> EnsureUniquePlantNames(
            List<PlantRecommendationDto> selected,
            IEnumerable<PlantRecommendationDto> fallbackPool,
            int limit)
        {
            if (selected.Count <= 1 || limit <= 0)
            {
                return selected;
            }

            var unique = new List<PlantRecommendationDto>(limit);
            var seenEntities = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var seenPlantNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            bool TryAdd(PlantRecommendationDto candidate)
            {
                if (unique.Count >= limit)
                {
                    return false;
                }

                var entityKey = $"{candidate.EntityType}:{candidate.EntityId}";
                if (!seenEntities.Add(entityKey))
                {
                    return false;
                }

                var nameKey = NormalizePlantName(candidate.Name);
                if (!string.IsNullOrEmpty(nameKey) && !seenPlantNames.Add(nameKey))
                {
                    return false;
                }

                unique.Add(candidate);
                return true;
            }

            foreach (var candidate in selected)
            {
                TryAdd(candidate);
            }

            if (unique.Count < limit)
            {
                foreach (var candidate in fallbackPool)
                {
                    TryAdd(candidate);
                    if (unique.Count >= limit)
                    {
                        break;
                    }
                }
            }

            return unique;
        }

        private static string? ResolveTargetFengShuiElement(string? requestedFengShuiElement)
        {
            return string.IsNullOrWhiteSpace(requestedFengShuiElement)
                ? null
                : NormalizeFengShuiElement(requestedFengShuiElement);
        }

        private static List<PlantRecommendationDto> EnsureHintCoverage(
            List<PlantRecommendationDto> selected,
            IEnumerable<PlantRecommendationDto> candidatePool,
            RoomAnalysisDto roomAnalysis,
            string? targetElement,
            int limit)
        {
            if (selected.Count == 0 || limit <= 0)
            {
                return selected;
            }

            var hintText = $"{roomAnalysis.Summary} {roomAnalysis.AvailableSpace}";
            var normalizedHintText = NormalizeTextForHintMatch(hintText);
            if (string.IsNullOrWhiteSpace(normalizedHintText))
            {
                return selected;
            }

            var selectedKeys = new HashSet<string>(
                selected.Select(c => $"{c.EntityType}:{c.EntityId}"),
                StringComparer.OrdinalIgnoreCase);

            var hintedInSelected = selected
                .Count(c => IsHintCandidateForSelection(c, normalizedHintText, targetElement));

            var hintedPool = candidatePool
                .Where(c => IsHintCandidateForSelection(c, normalizedHintText, targetElement))
                .Where(c => !selectedKeys.Contains($"{c.EntityType}:{c.EntityId}"))
                .ToList();

            var totalHintedAvailable = hintedInSelected + hintedPool.Count;
            if (totalHintedAvailable == 0)
            {
                return selected;
            }

            var desiredHintCount = Math.Min(limit >= 3 ? 2 : 1, totalHintedAvailable);
            if (hintedInSelected >= desiredHintCount)
            {
                return selected;
            }

            var stillNeeded = desiredHintCount - hintedInSelected;
            for (var i = 0; i < stillNeeded && hintedPool.Count > 0; i++)
            {
                var replacementIndex = selected
                    .Select((candidate, index) => new { Candidate = candidate, Index = index })
                    .Where(x => !IsHintCandidateForSelection(x.Candidate, normalizedHintText, targetElement))
                    .OrderBy(x => x.Candidate.MatchScore)
                    .Select(x => x.Index)
                    .FirstOrDefault(-1);

                if (replacementIndex < 0)
                {
                    break;
                }

                var replacement = hintedPool[0];
                hintedPool.RemoveAt(0);

                var removedKey = $"{selected[replacementIndex].EntityType}:{selected[replacementIndex].EntityId}";
                selectedKeys.Remove(removedKey);

                selected[replacementIndex] = replacement;
                selectedKeys.Add($"{replacement.EntityType}:{replacement.EntityId}");
            }

            return selected;
        }

        private static bool IsHintCandidateForSelection(
            PlantRecommendationDto candidate,
            string normalizedHintText,
            string? targetElement)
        {
            if (!IsCandidateMentionedInHints(candidate.Name, normalizedHintText))
            {
                return false;
            }

            return string.IsNullOrWhiteSpace(targetElement) || IsFengShuiMatchNormalized(candidate.FengShuiElement, targetElement);
        }

        private static List<PlantRecommendationDto> AlignToTargetFengShuiElement(
            List<PlantRecommendationDto> selected,
            IEnumerable<PlantRecommendationDto> candidatePool,
            string? targetElement,
            int limit)
        {
            if (selected.Count == 0 || string.IsNullOrWhiteSpace(targetElement) || limit <= 0)
            {
                return selected;
            }

            var selectedKeys = new HashSet<string>(
                selected.Select(c => $"{c.EntityType}:{c.EntityId}"),
                StringComparer.OrdinalIgnoreCase);

            var replacements = candidatePool
                .Where(c => IsFengShuiMatchNormalized(c.FengShuiElement, targetElement))
                .Where(c => !selectedKeys.Contains($"{c.EntityType}:{c.EntityId}"))
                .ToList();

            var replacementCursor = 0;
            for (var i = 0; i < selected.Count && replacementCursor < replacements.Count; i++)
            {
                if (IsFengShuiMatchNormalized(selected[i].FengShuiElement, targetElement))
                {
                    continue;
                }

                var removedKey = $"{selected[i].EntityType}:{selected[i].EntityId}";
                selectedKeys.Remove(removedKey);

                selected[i] = replacements[replacementCursor++];
                selectedKeys.Add($"{selected[i].EntityType}:{selected[i].EntityId}");
            }

            return selected.Take(limit).ToList();
        }

        private static bool IsFengShuiMatchNormalized(string? candidateElement, string targetElement)
        {
            if (string.IsNullOrWhiteSpace(candidateElement) || string.IsNullOrWhiteSpace(targetElement))
            {
                return false;
            }

            var normalizedCandidate = NormalizeFengShuiElement(candidateElement);
            return IsFengShuiCompatibleNormalized(normalizedCandidate, targetElement);
        }

        private static bool IsFengShuiCompatibleNormalized(string normalizedCandidate, string normalizedRequested)
        {
            if (normalizedCandidate == normalizedRequested)
            {
                return true;
            }

            return normalizedRequested switch
            {
                "KIM" => normalizedCandidate == "THO",
                "MOC" => normalizedCandidate == "THUY",
                "THUY" => normalizedCandidate == "KIM",
                "HOA" => normalizedCandidate == "MOC",
                "THO" => normalizedCandidate == "HOA",
                _ => false
            };
        }

        private static string NormalizePlantName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return string.Empty;
            }

            var decomposed = name.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);

            foreach (var ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(char.ToUpperInvariant(ch));
                }
            }

            return sb.ToString();
        }

        private static List<PlantRecommendationDto> PrioritizeCandidatesByRoomHints(
            IEnumerable<PlantRecommendationDto> candidates,
            RoomAnalysisDto roomAnalysis)
        {
            var hintText = $"{roomAnalysis.Summary} {roomAnalysis.AvailableSpace}";
            var normalizedHintText = NormalizeTextForHintMatch(hintText);

            var hinted = new List<PlantRecommendationDto>();
            var others = new List<PlantRecommendationDto>();

            foreach (var candidate in candidates)
            {
                if (IsCandidateMentionedInHints(candidate.Name, normalizedHintText))
                {
                    hinted.Add(candidate);
                }
                else
                {
                    others.Add(candidate);
                }
            }

            hinted.AddRange(others);
            return hinted;
        }

        private static bool IsCandidateMentionedInHints(string? candidateName, string normalizedHintText)
        {
            if (string.IsNullOrWhiteSpace(candidateName) || string.IsNullOrWhiteSpace(normalizedHintText))
            {
                return false;
            }

            var baseName = candidateName.Split('(')[0].Trim();
            var normalizedBaseName = NormalizeTextForHintMatch(baseName);
            if (!string.IsNullOrWhiteSpace(normalizedBaseName) && normalizedHintText.Contains(normalizedBaseName, StringComparison.Ordinal))
            {
                return true;
            }

            var tokens = normalizedBaseName
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length >= 2)
            {
                var firstTwoWords = string.Join(' ', tokens.Take(2));
                if (firstTwoWords.Length >= 4 && normalizedHintText.Contains(firstTwoWords, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeTextForHintMatch(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(decomposed.Length);

            foreach (var ch in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark)
                {
                    continue;
                }

                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(char.ToLowerInvariant(ch));
                }
                else
                {
                    sb.Append(' ');
                }
            }

            return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private static void NormalizeRequestFilters(RoomDesignRequestDto request)
        {
            if (request.MaxBudget.HasValue && request.MaxBudget.Value <= 0)
            {
                request.MaxBudget = null;
            }

            if (request.PreferredNurseryIds != null)
            {
                var normalizedNurseryIds = request.PreferredNurseryIds
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                request.PreferredNurseryIds = normalizedNurseryIds.Count > 0
                    ? normalizedNurseryIds
                    : null;
            }
        }

        private string MapRoomType(string? roomType)
        {
            return roomType?.ToLower() switch
            {
                "living_room" => "Phòng khách",
                "bedroom" => "Phòng ngủ",
                "office" => "Văn phòng",
                "kitchen" => "Nhà bếp",
                "bathroom" => "Phòng tắm",
                "balcony" => "Ban công",
                "garden" => "Sân vườn",
                _ => roomType ?? "Phòng"
            };
        }

    }
}
