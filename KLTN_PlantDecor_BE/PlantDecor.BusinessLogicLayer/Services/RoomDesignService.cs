using Microsoft.Extensions.Logging;
using Pgvector;
using PlantDecor.BusinessLogicLayer.Constants;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
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
        private static readonly HashSet<string> AllergyNoiseTokens = new(StringComparer.Ordinal)
        {
            "toi", "bi", "di", "ung", "voi", "cay", "nhung", "cac", "loai", "la", "va", "and", "khong"
        };

        private readonly IUnitOfWork _unitOfWork;
        private readonly IAzureOpenAIService _azureOpenAIService;
        private readonly IAISearchService _aiSearchService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<RoomDesignService> _logger;

        private const string ROOM_ANALYSIS_PROMPT = @"
Bạn là chuyên gia thiết kế nội thất và cây cảnh. Hãy phân tích ảnh căn phòng này và trả về JSON với cấu trúc sau:
{
    ""roomType"": ""LivingRoom|Bedroom|Kitchen|Bathroom|HomeOffice|Balcony|Corridor|DiningRoom"",
    ""roomSize"": ""small|medium|large"",
    ""lightingCondition"": ""LowLight|IndirectLight|PartialSun|FullSun"",
    ""interiorStyle"": ""Minimalist|Scandinavian|Tropical|Industrial|Bohemian|Modern|Japanese|Mediterranean|Rustic"",
    ""availableSpace"": ""floor|table|shelf|hanging|windowsill|corner"",
    ""colorPalette"": [""color1"", ""color2"", ""color3""],
    ""placementSuggestions"": [""suggestion1"", ""suggestion2""],
    ""summary"": ""Tóm tắt ngắn gọn về căn phòng và loại cây phù hợp""
}

Lưu ý:
- Đánh giá ánh sáng dựa trên cửa sổ, đèn, góc chụp
- Trường lightingCondition phải được suy luận từ ảnh phòng, không dựa vào thông tin do người dùng nhập
- Xác định không gian trống có thể đặt cây
- Đề xuất vị trí đặt cây phù hợp với phong thủy và thẩm mỹ
- Trường roomType, interiorStyle, lightingCondition PHẢI dùng đúng các giá trị enum đã liệt kê ở trên
- Hệ thống dùng trạng thái theo enum: LayoutDesignStatus(Processing|Completed|Failed) và RoomUploadModerationStatus(Pending|Approved|Rejected), không cần trả về 2 field này
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
            ICloudinaryService cloudinaryService,
            ILogger<RoomDesignService> logger)
        {
            _unitOfWork = unitOfWork;
            _azureOpenAIService = azureOpenAIService;
            _aiSearchService = aiSearchService;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        public async Task<PaginatedResult<LayoutDesignListResponseDto>> GetAllLayoutsAsync(int userId, Pagination pagination)
        {
            if (userId <= 0)
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }

            var paginatedLayouts = await _unitOfWork.LayoutDesignRepository.GetAllByUserIdWithDetailsAsync(userId, pagination);

            var layoutDtos = paginatedLayouts.Items.ToLayoutDesignListResponseList();
            return new PaginatedResult<LayoutDesignListResponseDto>(
                layoutDtos,
                paginatedLayouts.TotalCount,
                paginatedLayouts.PageNumber,
                paginatedLayouts.PageSize);
        }

        public async Task<RoomDesignResponseDto> AnalyzeAndRecommendUploadAsync(AnalyzeAndRecommendUploadRequest request, int userId)
        {
            if (request == null)
            {
                const string message = "Request body is required";
                var status = IsImageRelatedError(message)
                    ? RoomUploadModerationStatusEnum.Rejected
                    : RoomUploadModerationStatusEnum.Approved;

                await SaveRoomUploadModerationAsync(null, status, message);
                throw new BadRequestException(message);
            }

            if (!Enum.IsDefined(typeof(RoomTypeEnum), request.RoomType))
            {
                const string message = "RoomType is required";
                var status = IsImageRelatedError(message)
                    ? RoomUploadModerationStatusEnum.Rejected
                    : RoomUploadModerationStatusEnum.Approved;

                await SaveRoomUploadModerationAsync(null, status, message);
                throw new BadRequestException(message);
            }

            if (!Enum.IsDefined(typeof(RoomStyleEnum), request.RoomStyle))
            {
                const string message = "RoomStyle is required";
                var status = IsImageRelatedError(message)
                    ? RoomUploadModerationStatusEnum.Rejected
                    : RoomUploadModerationStatusEnum.Approved;

                await SaveRoomUploadModerationAsync(null, status, message);
                throw new BadRequestException(message);
            }

            if (request.HasAllergy != true && !string.IsNullOrWhiteSpace(request.AllergyNote))
            {
                const string message = "AllergyNote is only allowed when HasAllergy is true";
                var status = IsImageRelatedError(message)
                    ? RoomUploadModerationStatusEnum.Rejected
                    : RoomUploadModerationStatusEnum.Approved;

                await SaveRoomUploadModerationAsync(null, status, message);
                throw new BadRequestException(message);
            }

            if (request.HasAllergy != true && request.AllergicPlantIds?.Any() == true)
            {
                const string message = "AllergicPlantIds is only allowed when HasAllergy is true";
                var status = IsImageRelatedError(message)
                    ? RoomUploadModerationStatusEnum.Rejected
                    : RoomUploadModerationStatusEnum.Approved;

                await SaveRoomUploadModerationAsync(null, status, message);
                throw new BadRequestException(message);
            }

            if (request.Image == null || request.Image.Length == 0)
            {
                const string message = "Room image file is required";
                var status = IsImageRelatedError(message)
                    ? RoomUploadModerationStatusEnum.Rejected
                    : RoomUploadModerationStatusEnum.Approved;

                await SaveRoomUploadModerationAsync(null, status, message);
                throw new BadRequestException(message);
            }

            var (isValid, errorMessage) = _cloudinaryService.ValidateDocumentFile(request.Image);
            if (!isValid)
            {
                var message = string.IsNullOrWhiteSpace(errorMessage)
                    ? "Invalid room image file"
                    : errorMessage;
                var status = IsImageRelatedError(message)
                    ? RoomUploadModerationStatusEnum.Rejected
                    : RoomUploadModerationStatusEnum.Approved;

                await SaveRoomUploadModerationAsync(null, status, message);
                throw new BadRequestException(message);
            }

            RoomImage? roomImage = null;
            try
            {
                var uploadedImage = await _cloudinaryService.UploadFileAsync(request.Image, "RoomImages");

                roomImage = new RoomImage
                {
                    UserId = userId,
                    ImageUrl = uploadedImage.SecureUrl,
                    UploadedAt = DateTime.UtcNow
                };

                _unitOfWork.RoomImageRepository.PrepareCreate(roomImage);
                await _unitOfWork.SaveAsync();

                await using var stream = request.Image.OpenReadStream();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);

                var dto = new RoomDesignRequestDto
                {
                    RoomImageBase64 = Convert.ToBase64String(memoryStream.ToArray()),
                    FengShuiElement = request.FengShuiElement,
                    RoomType = request.RoomType,
                    RoomStyle = request.RoomStyle,
                    MinBudget = request.MinBudget,
                    MaxBudget = request.MaxBudget,
                    CareLevelType = request.CareLevelType,
                    HasAllergy = request.HasAllergy,
                    AllergyNote = request.AllergyNote,
                    AllergicPlantIds = request.AllergicPlantIds,
                    PetSafe = request.PetSafe,
                    ChildSafe = request.ChildSafe,
                    PreferredNurseryIds = request.PreferredNurseryIds,
                    RoomImageId = roomImage.Id,
                    UserId = userId,
                    UploadedImageUrl = uploadedImage.SecureUrl
                };

                var result = await AnalyzeAndRecommendAsync(dto, inferNaturalLightFromAi: true);
                await SaveRoomUploadModerationAsync(roomImage.Id, RoomUploadModerationStatusEnum.Approved, "Image validated successfully");
                return result;
            }
            catch (BadRequestException ex)
            {
                var moderationStatus = IsImageRelatedError(ex.Message)
                    ? RoomUploadModerationStatusEnum.Rejected
                    : RoomUploadModerationStatusEnum.Approved;

                await SaveRoomUploadModerationAsync(roomImage?.Id, moderationStatus, ex.Message);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing room image upload");
                var moderationStatus = IsImageRelatedError(ex.Message)
                    ? RoomUploadModerationStatusEnum.Rejected
                    : RoomUploadModerationStatusEnum.Approved;

                await SaveRoomUploadModerationAsync(roomImage?.Id, moderationStatus, ex.Message);
                throw;
            }
        }

        public async Task<RoomDesignResponseDto> AnalyzeAndRecommendAsync(
            RoomDesignRequestDto request,
            bool inferNaturalLightFromAi = false)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                ValidateRoomDesignRequest(request);

                var fengShuiFilter = request.FengShuiElement?.ToString();
                NormalizeRequestFilters(request);

                _logger.LogInformation(
                    "Room design filters normalized. MinBudget={MinBudget}, MaxBudget={MaxBudget}, PreferredNurseryIds={PreferredNurseryIds}, FengShuiElement={FengShuiElement}, PetSafe={PetSafe}, ChildSafe={ChildSafe}",
                    request.MinBudget,
                    request.MaxBudget,
                    request.PreferredNurseryIds == null ? "null" : string.Join(",", request.PreferredNurseryIds),
                    fengShuiFilter,
                    request.PetSafe,
                    request.ChildSafe);

                // Step 1: Analyze room image using Vision API
                _logger.LogInformation("Starting room analysis...");
                var roomAnalysis = await AnalyzeRoomAsync(request.RoomImageBase64);

                if (inferNaturalLightFromAi && !request.NaturalLightLevel.HasValue)
                {
                    var inferredNaturalLightLevel = MapLightingConditionToLightRequirement(roomAnalysis.LightingCondition);
                    if (inferredNaturalLightLevel.HasValue)
                    {
                        request.NaturalLightLevel = inferredNaturalLightLevel.Value;
                        _logger.LogInformation(
                            "NaturalLightLevel inferred from AI room analysis. LightingCondition={LightingCondition}, NaturalLightLevel={NaturalLightLevel}",
                            roomAnalysis.LightingCondition,
                            request.NaturalLightLevel.Value);
                    }
                }

                ApplyRequestPreferencesToRoomAnalysis(roomAnalysis, request);

                // Step 2: Build search query based on room analysis
                var searchQuery = BuildSearchQuery(roomAnalysis, request);
                _logger.LogInformation("Search query: {Query}", searchQuery);

                var allergyExclusionContext = await BuildAllergyExclusionContextAsync(request);

                // Step 3: Search for plants in database using embeddings
                var candidatePlants = await SearchCandidatePlantsAsync(searchQuery, request, allergyExclusionContext);

                // Step 4: If we have enough candidates, use AI to re-rank and explain
                var recommendations = await RankAndExplainRecommendationsAsync(
                    roomAnalysis,
                    candidatePlants,
                    request,
                    fengShuiFilter,
                    allergyExclusionContext);

                // Overwrite AI vision summary with a grounded summary based on DB-backed recommendations.
                roomAnalysis.Summary = BuildGroundedSummary(roomAnalysis, recommendations);

                var layoutDesignId = await PersistDesignArtifactsAsync(request, roomAnalysis, recommendations, searchQuery);

                stopwatch.Stop();

                return new RoomDesignResponseDto
                {
                    RoomAnalysis = roomAnalysis,
                    Recommendations = recommendations,
                    TotalCount = recommendations.Count,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    UserId = request.UserId,
                    LayoutDesignId = layoutDesignId
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
                    LightingCondition = "IndirectLight",
                    InteriorStyle = "Modern",
                    AvailableSpace = "floor",
                    ColorPalette = new List<string>(),
                    Summary = "Không thể phân tích chi tiết, đề xuất cây phù hợp chung"
                };
            }
        }

        public async Task<List<AllergyPlantOptionDto>> GetAllergyPlantOptionsAsync(string? keyword = null, int take = 50)
        {
            var normalizedKeyword = NormalizeTextForHintMatch(keyword);
            var normalizedTake = Math.Clamp(take, 1, 200);

            var activePlantCatalog = await GetActivePlantCatalogAsync();

            return activePlantCatalog
                .Where(item => string.IsNullOrWhiteSpace(normalizedKeyword) ||
                               item.NormalizedAliases.Any(alias => alias.Contains(normalizedKeyword, StringComparison.Ordinal)))
                .OrderBy(item => item.PlantName, StringComparer.OrdinalIgnoreCase)
                .Take(normalizedTake)
                .Select(item => new AllergyPlantOptionDto
                {
                    PlantId = item.PlantId,
                    PlantName = item.PlantName
                })
                .ToList();
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
                "LowLight" or "low" => "cây chịu bóng, ít ánh sáng",
                "IndirectLight" or "medium" => "cây bán bóng",
                "PartialSun" or "high" => "cây ưa sáng nhẹ, cần nắng một phần",
                "FullSun" or "natural" => "cây ưa nắng trực tiếp, cần nhiều ánh sáng",
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
                "Minimalist" or "minimalist" or "Scandinavian" or "scandinavian" => "cây đơn giản, thanh lịch",
                "Tropical" or "tropical" => "cây nhiệt đới, lá lớn",
                "Industrial" or "industrial" => "cây công nghiệp, xương rồng, sen đá",
                "Bohemian" or "bohemian" => "cây dây leo, cây treo",
                "Modern" or "modern" => "cây dáng gọn, hiện đại, tông xanh trung tính",
                "Japanese" or "japanese" => "cây phong cách zen, bố cục gọn gàng",
                "Mediterranean" or "mediterranean" => "cây thảo mộc, cây ưa sáng, phong cách địa trung hải",
                "Rustic" or "rustic" => "cây mộc mạc, tự nhiên, dễ chăm sóc",
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
            RoomDesignRequestDto request,
            AllergyExclusionContext allergyExclusionContext)
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
            // danh sách type gồm CommonPlant và PlantInstance
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
                // sau khi search xong thì trong metadata đã được update thêm field "CosineSimilarityScore" để dùng làm feature khi re-rank với AI
                var rawEmbeddings = await _unitOfWork.EmbeddingRepository
                    .SearchSimilarAsync(vector, perTypeLimit, entityType);

                // tách thành 2 list embeddings có type là commonPlant và plantInstance để sau đó khi chọn lọc candidate sẽ ưu tiên đa dạng loại cây hơn là chỉ tập trung vào 1 loại có thể chiếm hết top-k
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
                    // Danh sách embedding đã được phân loại theo entityType, nên khi lấy ra sẽ có 2 list riêng biệt cho CommonPlant và PlantInstance. Việc này giúp đảm bảo rằng khi chọn lọc candidate ở bước sau sẽ có sự đa dạng giữa cây trong catalog (CommonPlant) và cây thực tế đã bán (PlantInstance), thay vì chỉ tập trung vào một loại có thể chiếm hết top-k.
                    var typedEmbeddings = embeddingsByType[entityType];
                    // nếu index vượt quá số lượng embedding của type này thì bỏ qua, tiếp tục lấy embedding của type khác. Việc này giúp đảm bảo rằng nếu một type có ít embedding hơn perTypeLimit thì vẫn có thể lấy đủ số lượng candidate cần thiết từ type còn lại.
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
            var rejectedLightRequirement = 0;
            var rejectedLightRequirementFromMetadata = 0;
            var rejectedLightRequirementFromEntity = 0;
            var deferredLightRequirementChecks = 0;
            var rejectedCareLevel = 0;
            var rejectedPetSafe = 0;
            var rejectedChildSafe = 0;
            var rejectedAllergy = 0;
            var boostedLightMatch = 0;
            var penalizedMissingLightMetadata = 0;

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

                var guideLightRequirement = ExtractGuideLightRequirement(embedding.Metadata);
                if (request.NaturalLightLevel.HasValue && !guideLightRequirement.HasValue)
                {
                    deferredLightRequirementChecks++;
                }

                if (!IsEmbeddingLightRequirementMatch(embedding.Metadata, request.NaturalLightLevel))
                {
                    rejectedLightRequirement++;
                    rejectedLightRequirementFromMetadata++;
                    continue;
                }

                var adjustedScore = CalculateEmbeddingSearchScore(
                    embedding.Metadata,
                    request.NaturalLightLevel,
                    out var isLightMatched,
                    out var isMissingLightMetadata);

                if (isLightMatched)
                {
                    boostedLightMatch++;
                }

                if (isMissingLightMetadata)
                {
                    penalizedMissingLightMetadata++;
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

                if (!IsCandidateLightRequirementMatch(candidate.LightRequirement, request.NaturalLightLevel))
                {
                    rejectedLightRequirement++;
                    rejectedLightRequirementFromEntity++;
                    continue;
                }

                // Apply filters
                if (request.MinBudget.HasValue && candidate.Price < request.MinBudget)
                {
                    rejectedBudget++;
                    continue;
                }

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

                if (!IsCareLevelMatch(candidate.CareDifficulty, request.CareLevelType))
                {
                    rejectedCareLevel++;
                    continue;
                }

                if (request.PetSafe == true && candidate.PetSafe != true)
                {
                    rejectedPetSafe++;
                    continue;
                }

                if (request.ChildSafe == true && candidate.ChildSafe != true)
                {
                    rejectedChildSafe++;
                    continue;
                }

                if (IsCandidateExcludedByAllergy(candidate, allergyExclusionContext))
                {
                    rejectedAllergy++;
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

                candidate.MatchScore = adjustedScore;

                candidates.Add(candidate);

                if (candidates.Count >= searchLimit)
                    break;
            }

            candidates = candidates
                .OrderByDescending(c => c.MatchScore)
                .ToList();

            _logger.LogInformation(
                "Room design candidate search completed. Query='{Query}', Embeddings={Embeddings}, Candidates={Candidates}, RejectedMissingOriginalId={RejectedMissingOriginalId}, RejectedDuplicate={RejectedDuplicate}, RejectedDuplicatePlantName={RejectedDuplicatePlantName}, RejectedNotPurchasable={RejectedNotPurchasable}, RejectedNullCandidate={RejectedNullCandidate}, RejectedBudget={RejectedBudget}, RejectedPreferredNursery={RejectedPreferredNursery}, RejectedCareLevel={RejectedCareLevel}, RejectedPetSafe={RejectedPetSafe}, RejectedChildSafe={RejectedChildSafe}, RejectedAllergy={RejectedAllergy}, RejectedFengShui={RejectedFengShui}, RejectedLightRequirement={RejectedLightRequirement}, RejectedLightRequirementFromMetadata={RejectedLightRequirementFromMetadata}, RejectedLightRequirementFromEntity={RejectedLightRequirementFromEntity}, DeferredLightRequirementChecks={DeferredLightRequirementChecks}, BoostedLightMatch={BoostedLightMatch}, PenalizedMissingLightMetadata={PenalizedMissingLightMetadata}",
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
                rejectedCareLevel,
                rejectedPetSafe,
                rejectedChildSafe,
                rejectedAllergy,
                rejectedFengShui,
                rejectedLightRequirement,
                rejectedLightRequirementFromMetadata,
                rejectedLightRequirementFromEntity,
                deferredLightRequirementChecks,
                boostedLightMatch,
                penalizedMissingLightMetadata);

            return candidates;
        }

        private async Task<List<PlantRecommendationDto>> RankAndExplainRecommendationsAsync(
            RoomAnalysisDto roomAnalysis,
            List<PlantRecommendationDto> candidates,
            RoomDesignRequestDto request,
            string? fengShuiFilter,
            AllergyExclusionContext allergyExclusionContext)
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

                if (allergyExclusionContext.HasAnyExclusion)
                {
                    additionalCriteria += $". Tuyệt đối không đề xuất cây thuộc danh sách dị ứng người dùng đã chọn hoặc nêu trong ghi chú dị ứng";
                }

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
            return lightingCondition?.Trim() switch
            {
                "LowLight" => "yếu",
                "IndirectLight" => "gián tiếp",
                "PartialSun" => "nắng một phần",
                "FullSun" => "nắng trực tiếp",
                // "low" => "thấp",
                // "medium" => "trung bình",
                // "high" => "cao",
                // "natural" => "tự nhiên",
                _ => lightingCondition ?? "không xác định"
            };
        }

        private static LightRequirementEnum? MapLightingConditionToLightRequirement(string? lightingCondition)
        {
            return lightingCondition?.Trim() switch
            {
                "LowLight" or "low" => LightRequirementEnum.LowLight,
                "IndirectLight" or "medium" => LightRequirementEnum.IndirectLight,
                "PartialSun" => LightRequirementEnum.PartialSun,
                "FullSun" or "high" or "natural" => LightRequirementEnum.FullSun,
                _ => null
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
                            PlantId = commonPlant.PlantId,
                            PetSafe = commonPlant.Plant.PetSafe,
                            ChildSafe = commonPlant.Plant.ChildSafe,
                            LightRequirement = commonPlant.Plant.PlantGuide?.LightRequirement,
                            Name = commonPlant.Plant.Name,
                            Description = commonPlant.Plant.Description,
                            Price = commonPlant.Plant.BasePrice,
                            ImageUrl = commonPlantImageUrl,
                            FengShuiElement = MapFengShuiElement(commonPlant.Plant.FengShuiElement),
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
                            PlantId = instance.PlantId,
                            PetSafe = instance.Plant.PetSafe,
                            ChildSafe = instance.Plant.ChildSafe,
                            LightRequirement = instance.Plant.PlantGuide?.LightRequirement,
                            Name = instance.Plant.Name,
                            Description = instance.Description ?? instance.Plant.Description,
                            Price = instance.SpecificPrice ?? instance.Plant.BasePrice,
                            ImageUrl = instanceImageUrl,
                            FengShuiElement = MapFengShuiElement(instance.Plant.FengShuiElement),
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

        private static bool IsEmbeddingLightRequirementMatch(
            Dictionary<string, object>? metadata,
            LightRequirementEnum? requestedLightRequirement)
        {
            // Nếu người dùng không yêu cầu mức độ ánh sáng cụ thể, không cần so khớp
            if (!requestedLightRequirement.HasValue)
            {
                return true;
            }

            // User has explicitly chosen light requirement => strict filtering.
            // If metadata is missing, defer strict filtering to entity data.
            var guideLightRequirement = ExtractGuideLightRequirement(metadata);
            if (!guideLightRequirement.HasValue)
            {
                return true;
            }

            return guideLightRequirement.Value == requestedLightRequirement.Value;
        }

        private static bool IsCandidateLightRequirementMatch(
            int? candidateLightRequirement,
            LightRequirementEnum? requestedLightRequirement)
        {
            if (!requestedLightRequirement.HasValue)
            {
                return true;
            }

            if (!candidateLightRequirement.HasValue ||
                !Enum.IsDefined(typeof(LightRequirementEnum), candidateLightRequirement.Value))
            {
                return false;
            }

            return (LightRequirementEnum)candidateLightRequirement.Value == requestedLightRequirement.Value;
        }

        private static double CalculateEmbeddingSearchScore(
            Dictionary<string, object>? metadata,
            LightRequirementEnum? requestedLightRequirement,
            out bool isLightMatched,
            out bool isMissingLightMetadata)
        {
            var score = ExtractCosineSimilarityScore(metadata);

            isLightMatched = false;
            isMissingLightMetadata = false;

            if (!requestedLightRequirement.HasValue)
            {
                return score;
            }

            var guideLightRequirement = ExtractGuideLightRequirement(metadata);
            if (!guideLightRequirement.HasValue)
            {
                isMissingLightMetadata = true;
                return score - 0.05;
            }

            if (guideLightRequirement.Value == requestedLightRequirement.Value)
            {
                isLightMatched = true;
                return score + 0.1;
            }

            return score;
        }

        private static double ExtractCosineSimilarityScore(Dictionary<string, object>? metadata)
        {
            if (metadata == null)
            {
                return 0;
            }

            return TryReadMetadataDouble(metadata, "CosineSimilarityScore", out var score)
                ? score
                : 0;
        }

        private static LightRequirementEnum? ExtractGuideLightRequirement(Dictionary<string, object>? metadata)
        {
            if (metadata == null)
            {
                return null;
            }

            if (TryReadMetadataInt(metadata, "GuideLightRequirement", out var rawValue) &&
                Enum.IsDefined(typeof(LightRequirementEnum), rawValue))
            {
                return (LightRequirementEnum)rawValue;
            }

            if (TryReadMetadataInt(metadata, "LightRequirement", out rawValue) &&
                Enum.IsDefined(typeof(LightRequirementEnum), rawValue))
            {
                return (LightRequirementEnum)rawValue;
            }

            if (metadata.TryGetValue("GuideLightRequirementName", out var nameObj) &&
                nameObj is string name &&
                Enum.TryParse<LightRequirementEnum>(name, true, out var parsedByName))
            {
                return parsedByName;
            }

            return null;
        }

        private static bool TryReadMetadataInt(Dictionary<string, object> metadata, string key, out int value)
        {
            value = 0;

            if (!metadata.TryGetValue(key, out var rawValue) || rawValue == null)
            {
                return false;
            }

            switch (rawValue)
            {
                case int intValue:
                    value = intValue;
                    return true;
                case long longValue when longValue <= int.MaxValue && longValue >= int.MinValue:
                    value = (int)longValue;
                    return true;
                case JsonElement jsonElement:
                    if (jsonElement.ValueKind == JsonValueKind.Number && jsonElement.TryGetInt32(out value))
                    {
                        return true;
                    }

                    if (jsonElement.ValueKind == JsonValueKind.String &&
                        int.TryParse(jsonElement.GetString(), out value))
                    {
                        return true;
                    }

                    return false;
                default:
                    return int.TryParse(rawValue.ToString(), out value);
            }
        }

        private static bool TryReadMetadataDouble(Dictionary<string, object> metadata, string key, out double value)
        {
            value = 0;

            if (!metadata.TryGetValue(key, out var rawValue) || rawValue == null)
            {
                return false;
            }

            switch (rawValue)
            {
                case double doubleValue:
                    value = doubleValue;
                    return true;
                case float floatValue:
                    value = floatValue;
                    return true;
                case decimal decimalValue:
                    value = (double)decimalValue;
                    return true;
                case int intValue:
                    value = intValue;
                    return true;
                case long longValue:
                    value = longValue;
                    return true;
                case JsonElement jsonElement:
                    if (jsonElement.ValueKind == JsonValueKind.Number && jsonElement.TryGetDouble(out value))
                    {
                        return true;
                    }

                    if (jsonElement.ValueKind == JsonValueKind.String &&
                        double.TryParse(jsonElement.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
                    {
                        return true;
                    }

                    return false;
                default:
                    return double.TryParse(rawValue.ToString(), NumberStyles.Any, CultureInfo.InvariantCulture, out value);
            }
        }

        private static string MapCareDifficulty(int? careLevelType)
        {
            if (!careLevelType.HasValue)
            {
                return "Unknown";
            }

            return Enum.IsDefined(typeof(CareLevelTypeEnum), careLevelType.Value)
                ? ((CareLevelTypeEnum)careLevelType.Value).ToString()
                : "Unknown";
        }

        private static bool IsCareLevelMatch(string? candidateCareDifficulty, CareLevelTypeEnum? requestedCareLevelType)
        {
            if (!requestedCareLevelType.HasValue)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(candidateCareDifficulty))
            {
                return false;
            }

            return candidateCareDifficulty.Equals(requestedCareLevelType.Value.ToString(), StringComparison.OrdinalIgnoreCase);
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

        private static bool IsCandidateExcludedByAllergy(
            PlantRecommendationDto candidate,
            AllergyExclusionContext allergyExclusionContext)
        {
            if (allergyExclusionContext.ExcludedPlantIds.Count > 0 &&
                candidate.PlantId.HasValue &&
                allergyExclusionContext.ExcludedPlantIds.Contains(candidate.PlantId.Value))
            {
                return true;
            }

            return IsCandidateNameExcludedByAllergyAliases(candidate.Name, allergyExclusionContext.ExcludedPlantAliases);
        }

        private static bool IsCandidateNameExcludedByAllergyAliases(
            string? candidateName,
            HashSet<string> excludedPlantAliases)
        {
            if (string.IsNullOrWhiteSpace(candidateName) || excludedPlantAliases.Count == 0)
            {
                return false;
            }

            var baseName = candidateName.Split('(')[0].Trim();
            var normalizedBaseName = NormalizeTextForHintMatch(baseName);
            if (string.IsNullOrWhiteSpace(normalizedBaseName))
            {
                return false;
            }

            if (excludedPlantAliases.Contains(normalizedBaseName))
            {
                return true;
            }

            var tokens = normalizedBaseName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length >= 2)
            {
                var firstTwoWords = string.Join(' ', tokens.Take(2));
                if (firstTwoWords.Length >= 4 && excludedPlantAliases.Contains(firstTwoWords))
                {
                    return true;
                }
            }

            foreach (var excludedAlias in excludedPlantAliases)
            {
                if (excludedAlias.Length < 3)
                {
                    continue;
                }

                if (normalizedBaseName.Contains(excludedAlias, StringComparison.Ordinal) ||
                    excludedAlias.Contains(normalizedBaseName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private async Task<AllergyExclusionContext> BuildAllergyExclusionContextAsync(RoomDesignRequestDto request)
        {
            var context = new AllergyExclusionContext();
            if (request.HasAllergy != true)
            {
                return context;
            }

            var activePlantCatalog = await GetActivePlantCatalogAsync();
            var activePlantById = activePlantCatalog
                .ToDictionary(item => item.PlantId, item => item);

            var selectedAllergicPlantIds = request.AllergicPlantIds?
                .Where(id => id > 0)
                .Distinct()
                .ToList() ?? new List<int>();

            if (selectedAllergicPlantIds.Count > 0)
            {
                var invalidPlantIds = selectedAllergicPlantIds
                    .Where(id => !activePlantById.ContainsKey(id))
                    .ToList();

                if (invalidPlantIds.Count > 0)
                {
                    throw new BadRequestException("AllergicPlantIds must contain active Plant IDs only");
                }

                foreach (var plantId in selectedAllergicPlantIds)
                {
                    context.ExcludedPlantIds.Add(plantId);
                    foreach (var alias in activePlantById[plantId].NormalizedAliases)
                    {
                        context.ExcludedPlantAliases.Add(alias);
                    }
                }
            }

            var allergyNameTerms = ParseAllergyNameTerms(request.AllergyNote);
            foreach (var term in allergyNameTerms)
            {
                context.ExcludedPlantAliases.Add(term);
            }

            if (allergyNameTerms.Count == 0)
            {
                return context;
            }

            foreach (var plant in activePlantCatalog)
            {
                var matched = plant.NormalizedAliases
                    .Any(alias => allergyNameTerms.Any(term => IsAllergyTermMatchAlias(term, alias)));

                if (!matched)
                {
                    continue;
                }

                context.ExcludedPlantIds.Add(plant.PlantId);
                foreach (var alias in plant.NormalizedAliases)
                {
                    context.ExcludedPlantAliases.Add(alias);
                }
            }

            return context;
        }

        private async Task<List<AllergyPlantCatalogItem>> GetActivePlantCatalogAsync()
        {
            var plants = await _unitOfWork.PlantRepository.GetAllAsync();

            return plants
                .Where(plant => plant.IsActive == true && !string.IsNullOrWhiteSpace(plant.Name))
                .Select(plant => new AllergyPlantCatalogItem
                {
                    PlantId = plant.Id,
                    PlantName = plant.Name.Trim(),
                    NormalizedAliases = BuildPlantAliasSet(plant.Name, plant.SpecificName)
                })
                .ToList();
        }

        private static HashSet<string> BuildPlantAliasSet(string? plantName, string? specificName)
        {
            var aliases = new HashSet<string>(StringComparer.Ordinal);

            void AddAlias(string? rawAlias)
            {
                var normalizedAlias = NormalizeTextForHintMatch(rawAlias);
                if (string.IsNullOrWhiteSpace(normalizedAlias))
                {
                    return;
                }

                aliases.Add(normalizedAlias);

                var tokens = normalizedAlias.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length >= 2)
                {
                    aliases.Add(string.Join(' ', tokens.Take(2)));
                }
            }

            AddAlias(plantName);
            AddAlias(specificName);

            return aliases;
        }

        private static HashSet<string> ParseAllergyNameTerms(string? allergyNote)
        {
            var terms = new HashSet<string>(StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(allergyNote))
            {
                return terms;
            }

            var normalizedNote = NormalizeTextForHintMatch(allergyNote)
                .Replace(" and ", ",", StringComparison.Ordinal)
                .Replace(" va ", ",", StringComparison.Ordinal);

            var segments = normalizedNote
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => segment.Trim())
                .Where(segment => !string.IsNullOrWhiteSpace(segment))
                .ToList();

            if (segments.Count == 0)
            {
                segments.Add(normalizedNote);
            }

            foreach (var segment in segments)
            {
                var tokens = segment
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(token => !AllergyNoiseTokens.Contains(token))
                    .ToList();

                if (tokens.Count == 0)
                {
                    continue;
                }

                var cleaned = string.Join(' ', tokens);
                if (cleaned.Length >= 3)
                {
                    terms.Add(cleaned);
                }

                if (tokens.Count >= 2)
                {
                    var firstTwoWords = string.Join(' ', tokens.Take(2));
                    if (firstTwoWords.Length >= 4)
                    {
                        terms.Add(firstTwoWords);
                    }
                }
            }

            return terms;
        }

        private static bool IsAllergyTermMatchAlias(string term, string alias)
        {
            if (string.IsNullOrWhiteSpace(term) || string.IsNullOrWhiteSpace(alias))
            {
                return false;
            }

            if (term.Equals(alias, StringComparison.Ordinal))
            {
                return true;
            }

            if ((term.Length >= 3 && alias.Contains(term, StringComparison.Ordinal)) ||
                (alias.Length >= 3 && term.Contains(alias, StringComparison.Ordinal)))
            {
                return true;
            }

            var termTokens = term.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var aliasTokens = alias.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (termTokens.Length == 0 || aliasTokens.Length == 0)
            {
                return false;
            }

            var aliasTokenSet = new HashSet<string>(aliasTokens, StringComparer.Ordinal);
            return termTokens.All(token => aliasTokenSet.Contains(token));
        }

        private sealed class AllergyExclusionContext
        {
            public HashSet<int> ExcludedPlantIds { get; } = new();
            public HashSet<string> ExcludedPlantAliases { get; } = new(StringComparer.Ordinal);
            public bool HasAnyExclusion => ExcludedPlantIds.Count > 0 || ExcludedPlantAliases.Count > 0;
        }

        private sealed class AllergyPlantCatalogItem
        {
            public int PlantId { get; init; }
            public string PlantName { get; init; } = string.Empty;
            public HashSet<string> NormalizedAliases { get; init; } = new(StringComparer.Ordinal);
        }

        private void ApplyRequestPreferencesToRoomAnalysis(RoomAnalysisDto roomAnalysis, RoomDesignRequestDto request)
        {
            roomAnalysis.RoomType = MapRoomType(request.RoomType.ToString());
            roomAnalysis.InteriorStyle = request.RoomStyle.ToString();

            if (request.NaturalLightLevel.HasValue)
            {
                roomAnalysis.LightingCondition = request.NaturalLightLevel.Value switch
                {
                    LightRequirementEnum.LowLight => "LowLight",
                    LightRequirementEnum.IndirectLight => "IndirectLight",
                    LightRequirementEnum.PartialSun => "PartialSun",
                    LightRequirementEnum.FullSun => "FullSun",
                    _ => roomAnalysis.LightingCondition
                };
            }
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

        private async Task<int?> PersistDesignArtifactsAsync(
            RoomDesignRequestDto request,
            RoomAnalysisDto roomAnalysis,
            IReadOnlyCollection<PlantRecommendationDto> recommendations,
            string searchQuery)
        {
            if (!request.RoomImageId.HasValue)
            {
                return null;
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var preferences = new RoomDesignPreferences
                {
                    RoomImageId = request.RoomImageId.Value,
                    RoomType = (int)request.RoomType,
                    RoomStyle = (int)request.RoomStyle,
                    RoomArea = request.RoomArea,
                    MinBudget = request.MinBudget,
                    MaxBudget = request.MaxBudget,
                    CareLevel = request.CareLevelType.HasValue ? (int)request.CareLevelType.Value : null,
                    IsOftenAway = request.IsOftenAway,
                    NaturalLightLevel = request.NaturalLightLevel.HasValue ? (int)request.NaturalLightLevel.Value : null,
                    HasAllergy = request.HasAllergy,
                    AllergyNote = TrimAndLimit(request.AllergyNote, 500)
                };

                _unitOfWork.RoomDesignPreferencesRepository.PrepareCreate(preferences);

                var layoutDesign = new LayoutDesign
                {
                    UserId = request.UserId,
                    RoomImageId = request.RoomImageId.Value,
                    PreviewImageUrl = TrimAndLimit(request.UploadedImageUrl, 512),
                    RawResponse = JsonSerializer.Serialize(new
                    {
                        roomAnalysis,
                        recommendations
                    }),
                    Status = (int)LayoutDesignStatusEnum.PlantRecommendationCompleted,
                    IsSaved = false,
                    CreatedAt = DateTime.UtcNow
                };

                _unitOfWork.LayoutDesignRepository.PrepareCreate(layoutDesign);
                await _unitOfWork.SaveAsync();

                foreach (var recommendation in recommendations)
                {
                    var layoutDesignPlant = new LayoutDesignPlant
                    {
                        LayoutDesignId = layoutDesign.Id,
                        CommonPlantId = ResolveCommonPlantId(recommendation),
                        PlantInstanceId = ResolvePlantInstanceId(recommendation),
                        PlantReason = TrimAndLimit(recommendation.ReasonForRecommendation, 500),
                        PlacementPosition = TrimAndLimit(recommendation.SuggestedPlacement, 255),
                        PlacementReason = TrimAndLimit(recommendation.ReasonForRecommendation, 500),
                        CreatedAt = DateTime.UtcNow
                    };

                    _unitOfWork.LayoutDesignPlantRepository.PrepareCreate(layoutDesignPlant);
                }

                await _unitOfWork.CommitTransactionAsync();
                return layoutDesign.Id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to persist room design artifacts for RoomImageId={RoomImageId}", request.RoomImageId);
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private async Task SaveRoomUploadModerationAsync(
            int? roomImageId,
            RoomUploadModerationStatusEnum status,
            string? reason)
        {
            try
            {
                var defaultReason = status == RoomUploadModerationStatusEnum.Approved
                    ? "Image validated successfully"
                    : "Invalid room image";

                var moderation = new RoomUploadModeration
                {
                    RoomImageId = roomImageId,
                    Status = (int)status,
                    Reason = TrimAndLimit(string.IsNullOrWhiteSpace(reason) ? defaultReason : reason, 255),
                    ReviewedAt = DateTime.UtcNow
                };

                _unitOfWork.RoomUploadModerationRepository.PrepareCreate(moderation);
                await _unitOfWork.SaveAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save room upload moderation. RoomImageId={RoomImageId}", roomImageId);
            }
        }

        private static bool IsImageRelatedError(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return false;
            }

            var normalized = message.Trim().ToLowerInvariant();
            var imageErrorKeywords = new[]
            {
                "image",
                "ảnh",
                "file",
                "upload",
                "base64",
                "format",
                "size",
                "extension",
                "cloudinary"
            };

            return imageErrorKeywords.Any(keyword => normalized.Contains(keyword, StringComparison.Ordinal));
        }

        private static int? ResolveCommonPlantId(PlantRecommendationDto recommendation)
        {
            if (!recommendation.EntityType.Equals(EmbeddingEntityTypes.CommonPlant, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return recommendation.ProductId ?? recommendation.EntityId;
        }

        private static int? ResolvePlantInstanceId(PlantRecommendationDto recommendation)
        {
            if (!recommendation.EntityType.Equals(EmbeddingEntityTypes.PlantInstance, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return recommendation.ProductId ?? recommendation.EntityId;
        }

        private static string? TrimAndLimit(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            return trimmed.Length <= maxLength
                ? trimmed
                : trimmed[..maxLength];
        }

        private static void ValidateRoomDesignRequest(RoomDesignRequestDto request)
        {
            if (!Enum.IsDefined(typeof(RoomTypeEnum), request.RoomType))
            {
                throw new BadRequestException("RoomType is required");
            }

            if (!Enum.IsDefined(typeof(RoomStyleEnum), request.RoomStyle))
            {
                throw new BadRequestException("RoomStyle is required");
            }

            if (request.HasAllergy != true && !string.IsNullOrWhiteSpace(request.AllergyNote))
            {
                throw new BadRequestException("AllergyNote is only allowed when HasAllergy is true");
            }

            if (request.HasAllergy != true && request.AllergicPlantIds?.Any() == true)
            {
                throw new BadRequestException("AllergicPlantIds is only allowed when HasAllergy is true");
            }
        }

        private static void NormalizeRequestFilters(RoomDesignRequestDto request)
        {
            if (request.MinBudget.HasValue && request.MinBudget.Value <= 0)
            {
                request.MinBudget = null;
            }

            if (request.MaxBudget.HasValue && request.MaxBudget.Value <= 0)
            {
                request.MaxBudget = null;
            }

            if (request.MinBudget.HasValue && request.MaxBudget.HasValue && request.MinBudget > request.MaxBudget)
            {
                (request.MinBudget, request.MaxBudget) = (request.MaxBudget, request.MinBudget);
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

            if (request.AllergicPlantIds != null)
            {
                var normalizedAllergicPlantIds = request.AllergicPlantIds
                    .Where(id => id > 0)
                    .Distinct()
                    .ToList();

                request.AllergicPlantIds = normalizedAllergicPlantIds.Count > 0
                    ? normalizedAllergicPlantIds
                    : null;
            }
        }

        private string MapRoomType(string? roomType)
        {
            return roomType?.Trim() switch
            {
                "LivingRoom" or "living_room" => "Phòng khách",
                "Bedroom" or "bedroom" => "Phòng ngủ",
                "Kitchen" or "kitchen" => "Phòng bếp",
                "Bathroom" or "bathroom" => "Phòng tắm",
                "HomeOffice" or "office" => "Phòng làm việc tại nhà",
                "Balcony" or "balcony" => "Ban công",
                "Corridor" or "corridor" => "Hành lang",
                "DiningRoom" or "dining_room" => "Phòng ăn",
                _ => roomType ?? "Phòng"
            };
        }

    }
}
