using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pgvector;
using PlantDecor.BusinessLogicLayer.Constants;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;
using System.Text.Json;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class AISearchService : IAISearchService
    {
        private const string ChatbotIntentPlantSelection = "plant_selection";
        private const string ChatbotIntentRoomEnvironment = "room_environment";
        private const string ChatbotIntentPlantCare = "plant_care";
        private const string ChatbotIntentGeneral = "general";
        private const string ChatbotIntentPolicySupport = "policy_support";
        private const int MaxPolicyExcerptChars = 220;
        private const int MaxAssistantReplyChars = 2400;
        private const int MaxCareTipsCount = 10;
        private const int MaxCareTipChars = 220;
        private const int MaxFollowUpsCount = 6;
        private const int MaxFollowUpChars = 180;
        private const int MaxPolicySourcesCount = 6;

        private readonly IUnitOfWork _unitOfWork;
        private readonly IAzureOpenAIService _azureOpenAIService;
        private readonly IPolicyKnowledgeService _policyKnowledgeService;
        private readonly ILogger<AISearchService> _logger;
        private readonly string _userPolicyDocumentPath;
        private readonly string _returnPolicyDocumentPath;
        private readonly int _chatHistoryMaxTurns;
        private readonly int _chatHistoryTokenBudget;
        private readonly int _chatHistoryTokenReserve;

        public AISearchService(
            IUnitOfWork unitOfWork,
            IAzureOpenAIService azureOpenAIService,
            IPolicyKnowledgeService policyKnowledgeService,
            IConfiguration configuration,
            ILogger<AISearchService> logger)
        {
            _unitOfWork = unitOfWork;
            _azureOpenAIService = azureOpenAIService;
            _policyKnowledgeService = policyKnowledgeService;
            _logger = logger;
            _userPolicyDocumentPath = configuration["SupportAndPolicy:UserPolicyDocumentPath"]
                ?? "Mục Chính sách người dùng trên website/app PlantDecor";
            _returnPolicyDocumentPath = configuration["SupportAndPolicy:ReturnPolicyDocumentPath"]
                ?? "Mục Chính sách hoàn trả trên website/app PlantDecor";

            _chatHistoryMaxTurns = GetPositiveInt(configuration["AIChatbot:HistoryMaxTurns"], 12);
            _chatHistoryTokenBudget = GetPositiveInt(configuration["AIChatbot:HistoryMaxInputTokens"], 1200);
            var configuredReserve = GetPositiveInt(configuration["AIChatbot:ReservedOutputTokens"], 400);
            _chatHistoryTokenReserve = Math.Min(configuredReserve, Math.Max(80, _chatHistoryTokenBudget - 120));
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

        public async Task<AIChatSessionResponseDto> CreateChatSessionAsync(int userId, string? title = null)
        {
            if (userId <= 0)
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }

            var session = await _unitOfWork.AIChatSessionRepository.CreateSessionAsync(userId, title);
            return new AIChatSessionResponseDto
            {
                SessionId = session.Id,
                Title = session.Title,
                StartedAt = session.StartedAt,
                Status = session.Status ?? (int)AIChatSessionStatusEnum.Active
            };
        }

        public async Task<AIChatbotResponseDto> ChatbotAsync(AIChatbotRequestDto request, int userId)
        {
            if (request == null)
            {
                throw new BadRequestException("Invalid request");
            }

            if (userId <= 0)
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }

            var userMessage = request.Message?.Trim() ?? string.Empty;
            var sessionId = await ResolveOrCreateSessionIdAsync(request, userId, userMessage);

            try
            {
                _logger.LogInformation(
                    "AI chat request received. SessionId={SessionId}, UserId={UserId}, MessageLength={MessageLength}",
                    sessionId,
                    userId,
                    userMessage.Length);

                if (string.IsNullOrWhiteSpace(userMessage) && string.IsNullOrWhiteSpace(request.RoomDescription))
                {
                    var validationFallback = ClampChatbotResponsePayload(BuildFallbackChatbotResponse(
                        ChatbotIntentGeneral,
                        new List<PlantSuggestionResponseDto>(),
                        null,
                        "Bạn hãy mô tả nhu cầu chọn cây, môi trường phòng hoặc vấn đề chăm sóc để mình tư vấn chính xác hơn nhé."));
                    await PersistAssistantMessageAsync(sessionId, userId, validationFallback, ChatbotIntentGeneral, true, false);
                    return validationFallback;
                }

                if (!string.IsNullOrWhiteSpace(userMessage))
                {
                    await _unitOfWork.AIChatMessageRepository.AddUserMessageAsync(sessionId, userId, userMessage);
                }

                var boundedHistory = await BuildBoundedSessionHistoryAsync(sessionId, userId);
                _logger.LogInformation(
                    "AI chat history prepared. SessionId={SessionId}, UserId={UserId}, TurnCount={TurnCount}",
                    sessionId,
                    userId,
                    boundedHistory.Count);

                var intentAnalysis = await AnalyzeChatIntentAsync(request, boundedHistory);
                var intent = NormalizeIntent(intentAnalysis.Intent);

                if (intent == ChatbotIntentPolicySupport)
                {
                    var policyResponse = ClampChatbotResponsePayload(await BuildPolicySupportResponseAsync());
                    _logger.LogInformation(
                        "AI policy branch routed. SessionId={SessionId}, UserId={UserId}, PolicySources={PolicySources}",
                        sessionId,
                        userId,
                        policyResponse.PolicySources.Count);
                    await PersistAssistantMessageAsync(sessionId, userId, policyResponse, ChatbotIntentPolicySupport, false, true);
                    return policyResponse;
                }

                var effectiveLimit = ClampLimit(request.Limit ?? intentAnalysis.RequestedPlantCount ?? 5, 1, 10);
                var effectiveFengShuiElement = FirstNonEmpty(request.FengShuiElement, intentAnalysis.FengShuiElement);
                var effectivePreferredRooms = ResolvePreferredRooms(request.PreferredRooms, intentAnalysis.PreferredRooms);
                var effectiveMaxBudget = request.MaxBudget ?? intentAnalysis.MaxBudget;
                var effectivePetSafe = request.PetSafe ?? intentAnalysis.PetSafe;
                var effectiveChildSafe = request.ChildSafe ?? intentAnalysis.ChildSafe;

                var roomSummary = FirstNonEmpty(request.RoomDescription, intentAnalysis.RoomSummary);
                var recommendationQuery = FirstNonEmpty(
                    roomSummary,
                    intentAnalysis.SearchQuery,
                    userMessage,
                    "Cây cảnh trong nhà dễ chăm sóc")
                    ?? "Cây cảnh trong nhà dễ chăm sóc";

                var suggestions = await GetChatbotSuggestionsAsync(
                    intent,
                    recommendationQuery,
                    effectiveFengShuiElement,
                    effectivePreferredRooms,
                    effectiveMaxBudget,
                    effectivePetSafe,
                    effectiveChildSafe,
                    request.OnlyPurchasable,
                    effectiveLimit,
                    intentAnalysis.RoomType);

                var plantGuideCareTips = await BuildCareTipsFromPlantGuidesAsync(userMessage, intent, suggestions);

                var answer = await GenerateChatbotAnswerAsync(
                    request,
                    intent,
                    roomSummary,
                    suggestions,
                    intentAnalysis,
                    boundedHistory,
                    effectiveFengShuiElement,
                    effectivePreferredRooms,
                    effectiveMaxBudget,
                    effectivePetSafe,
                    effectiveChildSafe);

                if (answer == null)
                {
                    var llmFallback = ClampChatbotResponsePayload(BuildFallbackChatbotResponse(intent, suggestions, roomSummary, null, plantGuideCareTips));
                    await PersistAssistantMessageAsync(sessionId, userId, llmFallback, intent, true, false);
                    return llmFallback;
                }

                var response = ClampChatbotResponsePayload(new AIChatbotResponseDto
                {
                    Intent = intent,
                    Reply = answer.Reply,
                    RoomEnvironmentSummary = roomSummary,
                    SuggestedPlants = suggestions,
                    CareTips = MergeCareTips(answer.CareTips, plantGuideCareTips),
                    FollowUpQuestions = answer.FollowUpQuestions ?? new List<string>(),
                    Disclaimer = answer.Disclaimer,
                    UsedFallback = false
                });

                _logger.LogInformation(
                    "AI chat response generated. SessionId={SessionId}, UserId={UserId}, Intent={Intent}, Suggestions={SuggestionCount}, UsedFallback={UsedFallback}",
                    sessionId,
                    userId,
                    response.Intent,
                    response.SuggestedPlants.Count,
                    response.UsedFallback);

                await PersistAssistantMessageAsync(sessionId, userId, response, intent, false, false);
                return response;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized AI chat session access. SessionId={SessionId}, UserId={UserId}", sessionId, userId);
                throw new ForbiddenException("You do not have permission to access this AI chat session.");
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Invalid AI chat session state. SessionId={SessionId}, UserId={UserId}", sessionId, userId);
                throw new BadRequestException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in AI chatbot flow");
                var fallback = ClampChatbotResponsePayload(BuildFallbackChatbotResponse(
                    ChatbotIntentGeneral,
                    new List<PlantSuggestionResponseDto>(),
                    request.RoomDescription,
                    "Hiện hệ thống AI đang bận, bạn thử lại sau ít phút hoặc gửi mô tả ngắn gọn hơn."));
                await PersistAssistantMessageAsync(sessionId, userId, fallback, ChatbotIntentGeneral, true, false);
                return fallback;
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

        private async Task<List<PlantSuggestionResponseDto>> GetChatbotSuggestionsAsync(
            string intent,
            string recommendationQuery,
            string? fengShuiElement,
            List<string>? preferredRooms,
            decimal? maxBudget,
            bool? petSafe,
            bool? childSafe,
            bool onlyPurchasable,
            int limit,
            string? roomType)
        {
            var requiresRoomAwareFiltering =
                intent == ChatbotIntentRoomEnvironment
                || petSafe == true
                || childSafe == true
                || (preferredRooms?.Any() ?? false);

            if (requiresRoomAwareFiltering)
            {
                var roomRecommendations = await GetRoomRecommendationsAsync(
                    roomDescription: recommendationQuery,
                    fengShuiElement: fengShuiElement,
                    maxBudget: maxBudget,
                    limit: limit,
                    preferredRooms: preferredRooms,
                    petSafe: petSafe,
                    childSafe: childSafe);

                return roomRecommendations.Recommendations
                    .Select(MapRoomRecommendationToSuggestion)
                    .Take(limit)
                    .ToList();
            }

            if (intent == ChatbotIntentPlantCare)
            {
                return await SuggestPlantsAsync(
                    description: recommendationQuery,
                    fengShuiElement: fengShuiElement,
                    roomType: roomType,
                    onlyPurchasable: onlyPurchasable,
                    limit: Math.Min(limit, 3),
                    maxBudget: maxBudget);
            }

            return await SuggestPlantsAsync(
                description: recommendationQuery,
                fengShuiElement: fengShuiElement,
                roomType: roomType,
                onlyPurchasable: onlyPurchasable,
                limit: limit,
                maxBudget: maxBudget);
        }

        private async Task<ChatbotIntentAnalysis> AnalyzeChatIntentAsync(
            AIChatbotRequestDto request,
            List<ChatbotConversationTurnDto> conversationHistory)
        {
            var fallback = new ChatbotIntentAnalysis
            {
                Intent = ChatbotIntentGeneral,
                SearchQuery = request.Message ?? string.Empty,
                RoomSummary = request.RoomDescription ?? string.Empty,
                FengShuiElement = request.FengShuiElement,
                PreferredRooms = request.PreferredRooms,
                PetSafe = request.PetSafe,
                ChildSafe = request.ChildSafe,
                MaxBudget = request.MaxBudget,
                RequestedPlantCount = request.Limit
            };

            try
            {
                var historyContext = BuildHistoryContext(conversationHistory);

                var parsePrompt =
                    "Bạn là bộ phân tích ý định cho chatbot cây cảnh. " +
                    "Hãy trả về DUY NHẤT một JSON object hợp lệ với các field: " +
                    "intent (plant_selection|room_environment|plant_care|policy_support|general), " +
                    "searchQuery, roomSummary, roomType, lightingCondition, fengShuiElement, " +
                    "preferredRooms (array string), petSafe (bool|null), childSafe (bool|null), " +
                    "maxBudget (number|null), requestedPlantCount (int|null), followUpQuestions (array string). " +
                    "Nếu không chắc, dùng null hoặc mảng rỗng. Không thêm markdown.";

                var payload = JsonSerializer.Serialize(new
                {
                    message = request.Message,
                    roomDescription = request.RoomDescription,
                    fengShuiElement = request.FengShuiElement,
                    maxBudget = request.MaxBudget,
                    preferredRooms = request.PreferredRooms,
                    petSafe = request.PetSafe,
                    childSafe = request.ChildSafe,
                    limit = request.Limit,
                    history = historyContext
                });

                var jsonResponse = await _azureOpenAIService.GenerateJsonResponseAsync(parsePrompt, payload);
                if (string.IsNullOrWhiteSpace(jsonResponse))
                {
                    return fallback;
                }

                var normalizedJson = ExtractJsonObject(jsonResponse);
                var parsed = JsonSerializer.Deserialize<ChatbotIntentAnalysis>(normalizedJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsed == null)
                {
                    return fallback;
                }

                parsed.Intent = NormalizeIntent(parsed.Intent);
                parsed.SearchQuery = FirstNonEmpty(parsed.SearchQuery, request.Message);
                parsed.RoomSummary = FirstNonEmpty(parsed.RoomSummary, request.RoomDescription);
                parsed.FengShuiElement = FirstNonEmpty(parsed.FengShuiElement, request.FengShuiElement);
                parsed.PreferredRooms = ResolvePreferredRooms(parsed.PreferredRooms, request.PreferredRooms);
                parsed.PetSafe ??= request.PetSafe;
                parsed.ChildSafe ??= request.ChildSafe;
                parsed.MaxBudget ??= request.MaxBudget;
                parsed.RequestedPlantCount ??= request.Limit;

                return parsed;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse chatbot intent. Fallback to default intent.");
                return fallback;
            }
        }

        private async Task<ChatbotAnswerPayload?> GenerateChatbotAnswerAsync(
            AIChatbotRequestDto request,
            string intent,
            string? roomSummary,
            List<PlantSuggestionResponseDto> suggestions,
            ChatbotIntentAnalysis intentAnalysis,
            List<ChatbotConversationTurnDto> conversationHistory,
            string? fengShuiElement,
            List<string>? preferredRooms,
            decimal? maxBudget,
            bool? petSafe,
            bool? childSafe)
        {
            try
            {
                var answerPrompt =
                    "Bạn là AI chatbot PlantDecor. Nhiệm vụ: tư vấn chọn cây, hiểu môi trường phòng cơ bản và tư vấn chăm sóc cây. " +
                    "Trả lời bằng tiếng Việt thân thiện, thực tế, không bịa sản phẩm ngoài dữ liệu. " +
                    "Hãy trả về DUY NHẤT JSON object với các field: reply (string), careTips (array string), " +
                    "followUpQuestions (array string), disclaimer (string|null). " +
                    "Nếu không có careTips hoặc followUpQuestions thì trả mảng rỗng.";

                var contextPayload = JsonSerializer.Serialize(new
                {
                    message = request.Message,
                    intent,
                    roomEnvironmentSummary = roomSummary,
                    constraints = new
                    {
                        fengShuiElement,
                        preferredRooms,
                        maxBudget,
                        petSafe,
                        childSafe,
                        onlyPurchasable = request.OnlyPurchasable
                    },
                    aiUnderstanding = new
                    {
                        intentAnalysis.RoomType,
                        intentAnalysis.LightingCondition,
                        intentAnalysis.FollowUpQuestions
                    },
                    suggestedPlants = suggestions.Select(s => new
                    {
                        s.EntityType,
                        s.EntityId,
                        s.Name,
                        s.Description,
                        s.Price,
                        s.RelevanceScore
                    }).ToList(),
                    history = BuildHistoryContext(conversationHistory)
                });

                var answerJson = await _azureOpenAIService.GenerateJsonResponseAsync(answerPrompt, contextPayload);
                if (string.IsNullOrWhiteSpace(answerJson))
                {
                    return null;
                }

                var normalizedJson = ExtractJsonObject(answerJson);
                var payload = JsonSerializer.Deserialize<ChatbotAnswerPayload>(normalizedJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (payload == null || string.IsNullOrWhiteSpace(payload.Reply))
                {
                    return null;
                }

                payload.CareTips = NormalizeTextList(payload.CareTips);
                payload.FollowUpQuestions = NormalizeTextList(payload.FollowUpQuestions);
                payload.Reply = payload.Reply.Trim();
                payload.Disclaimer = string.IsNullOrWhiteSpace(payload.Disclaimer) ? null : payload.Disclaimer.Trim();

                return payload;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate chatbot final answer from LLM");
                return null;
            }
        }

        private AIChatbotResponseDto BuildFallbackChatbotResponse(
            string intent,
            List<PlantSuggestionResponseDto> suggestions,
            string? roomSummary,
            string? fallbackReply,
            List<string>? plantGuideCareTips = null)
        {
            var suggestionText = suggestions.Any()
                ? $"Mình có {suggestions.Count} gợi ý phù hợp, bạn có thể xem danh sách cây bên dưới để chọn nhanh."
                : "Mình chưa tìm thấy cây phù hợp ngay bây giờ, bạn thử thêm thông tin về ánh sáng, ngân sách hoặc nhu cầu chăm sóc nhé.";

            var defaultCareTips = new List<string>
            {
                "Kiểm tra độ ẩm đất trước khi tưới, tránh tưới theo lịch cố định.",
                "Đặt cây ở nơi có ánh sáng phù hợp với từng loại cây, tránh nắng gắt trực tiếp cả ngày.",
                "Quan sát lá vàng hoặc úng để điều chỉnh lượng nước và thông gió."
            };

            return new AIChatbotResponseDto
            {
                Intent = NormalizeIntent(intent),
                Reply = fallbackReply ?? suggestionText,
                RoomEnvironmentSummary = roomSummary,
                SuggestedPlants = suggestions,
                CareTips = MergeCareTips(defaultCareTips, plantGuideCareTips),
                FollowUpQuestions = new List<string>
                {
                    "Phòng của bạn nhận ánh sáng trực tiếp hay ánh sáng tán xạ?",
                    "Bạn ưu tiên cây dễ chăm hay cây có giá trị thẩm mỹ cao?"
                },
                PolicySources = new List<PolicyGroundingSourceDto>(),
                Disclaimer = "Thông tin chỉ mang tính tham khảo. Nếu cây có dấu hiệu bệnh nặng, bạn nên liên hệ chuyên gia chăm sóc cây.",
                UsedFallback = true
            };
        }

        private async Task<AIChatbotResponseDto> BuildPolicySupportResponseAsync()
        {
            try
            {
                var userPolicies = await _policyKnowledgeService.GetByCategoryActiveAsync(PolicyContentCategoryEnum.UserPolicy);
                var returnPolicies = await _policyKnowledgeService.GetByCategoryActiveAsync(PolicyContentCategoryEnum.ReturnPolicy);
                var policySources = BuildPolicyGroundingSources(userPolicies, returnPolicies);

                var userPolicySection = BuildPolicySection("Chính sách người dùng", userPolicies);
                var returnPolicySection = BuildPolicySection("Chính sách hoàn trả", returnPolicies);

                if (string.IsNullOrWhiteSpace(userPolicySection) && string.IsNullOrWhiteSpace(returnPolicySection))
                {
                    return BuildPolicySupportFallbackResponse();
                }

                var policySections = new List<string>();
                if (!string.IsNullOrWhiteSpace(userPolicySection))
                {
                    policySections.Add(userPolicySection);
                }

                if (!string.IsNullOrWhiteSpace(returnPolicySection))
                {
                    policySections.Add(returnPolicySection);
                }

                var groundedSummary = string.Join("\n", policySections);

                return new AIChatbotResponseDto
                {
                    Intent = ChatbotIntentPolicySupport,
                    Reply =
                        "Với câu hỏi về chính sách người dùng hoặc hoàn trả, mình đã lấy thông tin từ nội dung chính sách đang active trong hệ thống:\n" +
                        groundedSummary + "\n" +
                        "Để đảm bảo thông tin cuối cùng là chính xác tại thời điểm giao dịch, bạn vui lòng chat với tư vấn viên. " +
                        $"Bạn cũng có thể xem chính sách tại {_userPolicyDocumentPath} và {_returnPolicyDocumentPath}.",
                    SuggestedPlants = new List<PlantSuggestionResponseDto>(),
                    CareTips = new List<string>(),
                    FollowUpQuestions = new List<string>
                    {
                        "Bạn cần mình trích ngắn hơn phần chính sách người dùng hay chính sách hoàn trả?",
                        "Bạn muốn mình nhắc lại là nên chat với tư vấn viên để xác nhận phiên bản chính sách mới nhất không?"
                    },
                    PolicySources = policySources,
                    Disclaimer = "Nội dung chính sách có thể thay đổi theo thời điểm, vui lòng xác nhận với tư vấn viên trước khi thực hiện giao dịch.",
                    UsedFallback = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to build DB-grounded policy response. Falling back to safe support response.");
                return BuildPolicySupportFallbackResponse();
            }
        }

        private AIChatbotResponseDto BuildPolicySupportFallbackResponse()
        {
            return new AIChatbotResponseDto
            {
                Intent = ChatbotIntentPolicySupport,
                Reply =
                    "Với câu hỏi về chính sách người dùng hoặc hoàn trả, mình cần chuyển bạn sang kênh hỗ trợ người thật để đảm bảo thông tin chính xác và cập nhật. " +
                    $"Bạn vui lòng chat trực tiếp với tư vấn viên trong mục Hỗ trợ, hoặc xem file/chuyên mục chính sách tại {_userPolicyDocumentPath} và {_returnPolicyDocumentPath}.",
                SuggestedPlants = new List<PlantSuggestionResponseDto>(),
                CareTips = new List<string>(),
                FollowUpQuestions = new List<string>
                {
                    "Bạn muốn mình nhắc lại là hãy vào mục Hỗ trợ để chat với tư vấn viên không?",
                    "Bạn đang cần chính sách người dùng hay chính sách hoàn trả?"
                },
                PolicySources = new List<PolicyGroundingSourceDto>(),
                Disclaimer = "Nội dung chính sách có thể thay đổi theo thời điểm, vui lòng xác nhận với tư vấn viên trước khi thực hiện giao dịch.",
                UsedFallback = false
            };
        }

        private static string BuildPolicySection(string sectionTitle, List<DataAccessLayer.Entities.PolicyContent>? policies)
        {
            if (policies == null || policies.Count == 0)
            {
                return string.Empty;
            }

            var excerpts = policies
                .Where(p => p.IsActive == true && !string.IsNullOrWhiteSpace(p.Content))
                .OrderBy(p => p.DisplayOrder ?? int.MaxValue)
                .ThenBy(p => p.Id)
                .Take(2)
                .Select(p =>
                {
                    var title = FirstNonEmpty(p.Title, sectionTitle, "Chính sách") ?? "Chính sách";
                    var sourceText = FirstNonEmpty(p.Summary, p.Content);
                    var excerpt = BuildPolicyExcerpt(sourceText, MaxPolicyExcerptChars);
                    return string.IsNullOrWhiteSpace(excerpt) ? null : $"- {title}: {excerpt}";
                })
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();

            if (excerpts.Count == 0)
            {
                return string.Empty;
            }

            return sectionTitle + ":\n" + string.Join("\n", excerpts!);
        }

        private static string BuildPolicyExcerpt(string? text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            var normalized = text
                .Replace("\r", " ")
                .Replace("\n", " ")
                .Trim();

            if (normalized.Length <= maxChars)
            {
                return normalized;
            }

            return normalized[..maxChars].TrimEnd() + "...";
        }

        private async Task<List<ChatbotConversationTurnDto>> BuildBoundedSessionHistoryAsync(int sessionId, int userId)
        {
            var repositoryPageSize = Math.Max(_chatHistoryMaxTurns * 4, 20);
            var messages = await _unitOfWork.AIChatMessageRepository.GetSessionMessagesAsync(sessionId, userId, 1, repositoryPageSize);

            var normalizedTurns = messages
                .Where(m => !string.IsNullOrWhiteSpace(m.Content)
                    && (m.Role == (int)AIChatMessageRoleEnum.User || m.Role == (int)AIChatMessageRoleEnum.Assistant))
                .OrderBy(m => m.CreatedAt ?? DateTime.MinValue)
                .Select(m => new ChatbotConversationTurnDto
                {
                    Role = m.Role == (int)AIChatMessageRoleEnum.Assistant ? "assistant" : "user",
                    Content = m.Content!.Trim()
                })
                .ToList();

            return TrimHistoryToWindow(normalizedTurns, _chatHistoryMaxTurns, _chatHistoryTokenBudget, _chatHistoryTokenReserve);
        }

        private async Task<int> ResolveOrCreateSessionIdAsync(AIChatbotRequestDto request, int userId, string userMessage)
        {
            if (request.SessionId > 0)
            {
                var session = await _unitOfWork.AIChatSessionRepository.GetByIdAndUserAsync(request.SessionId, userId);
                if (session == null)
                {
                    throw new NotFoundException("AI chat session not found.");
                }

                if (session.Status != (int)AIChatSessionStatusEnum.Active)
                {
                    throw new BadRequestException("AI chat session is closed.");
                }

                return session.Id;
            }

            var recentSessions = await _unitOfWork.AIChatSessionRepository.GetUserSessionsAsync(userId, 1, 10);
            var activeSession = recentSessions.FirstOrDefault(s => s.Status == (int)AIChatSessionStatusEnum.Active);
            if (activeSession != null)
            {
                return activeSession.Id;
            }

            var created = await _unitOfWork.AIChatSessionRepository.CreateSessionAsync(userId, BuildSessionTitle(userMessage));
            _logger.LogInformation("AI chat session auto-created. SessionId={SessionId}, UserId={UserId}", created.Id, userId);
            return created.Id;
        }

        private static string BuildSessionTitle(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return "Chatbot";
            }

            var normalized = message.Replace("\r", " ").Replace("\n", " ").Trim();
            if (normalized.Length <= 60)
            {
                return normalized;
            }

            return normalized[..60].TrimEnd() + "...";
        }

        private async Task PersistAssistantMessageAsync(
            int sessionId,
            int userId,
            AIChatbotResponseDto response,
            string intent,
            bool isFallback,
            bool isPolicyResponse)
        {
            await _unitOfWork.AIChatMessageRepository.AddAssistantMessageAsync(
                sessionId,
                userId,
                response.Reply,
                intent,
                isFallback,
                isPolicyResponse);

            _logger.LogInformation(
                "Assistant message persisted. SessionId={SessionId}, UserId={UserId}, Intent={Intent}, IsFallback={IsFallback}, IsPolicyResponse={IsPolicyResponse}, ReplyLength={ReplyLength}",
                sessionId,
                userId,
                intent,
                isFallback,
                isPolicyResponse,
                response.Reply.Length);
        }

        private static List<PolicyGroundingSourceDto> BuildPolicyGroundingSources(
            List<DataAccessLayer.Entities.PolicyContent>? userPolicies,
            List<DataAccessLayer.Entities.PolicyContent>? returnPolicies)
        {
            var merged = new List<DataAccessLayer.Entities.PolicyContent>();

            if (userPolicies != null)
            {
                merged.AddRange(userPolicies);
            }

            if (returnPolicies != null)
            {
                merged.AddRange(returnPolicies);
            }

            return merged
                .Where(p => p.IsActive == true && p.Id > 0)
                .OrderBy(p => p.Category ?? int.MaxValue)
                .ThenBy(p => p.DisplayOrder ?? int.MaxValue)
                .ThenBy(p => p.Id)
                .Take(MaxPolicySourcesCount)
                .Select(p => new PolicyGroundingSourceDto
                {
                    PolicyContentId = p.Id,
                    Category = p.Category,
                    Title = p.Title,
                    Excerpt = BuildPolicyExcerpt(FirstNonEmpty(p.Summary, p.Content), MaxPolicyExcerptChars)
                })
                .ToList();
        }

        private static AIChatbotResponseDto ClampChatbotResponsePayload(AIChatbotResponseDto response)
        {
            response.Reply = BuildPolicyExcerpt(response.Reply, MaxAssistantReplyChars);
            response.CareTips = ClampTextList(response.CareTips, MaxCareTipsCount, MaxCareTipChars);
            response.FollowUpQuestions = ClampTextList(response.FollowUpQuestions, MaxFollowUpsCount, MaxFollowUpChars);
            response.PolicySources = (response.PolicySources ?? new List<PolicyGroundingSourceDto>())
                .Take(MaxPolicySourcesCount)
                .Select(s => new PolicyGroundingSourceDto
                {
                    PolicyContentId = s.PolicyContentId,
                    Category = s.Category,
                    Title = string.IsNullOrWhiteSpace(s.Title) ? null : BuildPolicyExcerpt(s.Title, 120),
                    Excerpt = string.IsNullOrWhiteSpace(s.Excerpt) ? null : BuildPolicyExcerpt(s.Excerpt, MaxPolicyExcerptChars)
                })
                .ToList();

            return response;
        }

        private static List<string> ClampTextList(List<string>? values, int maxItems, int maxChars)
        {
            if (values == null || values.Count == 0)
            {
                return new List<string>();
            }

            return values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => BuildPolicyExcerpt(v, maxChars))
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(maxItems)
                .ToList();
        }

        private static PlantSuggestionResponseDto MapRoomRecommendationToSuggestion(PlantRecommendationItemDto recommendation)
        {
            return new PlantSuggestionResponseDto
            {
                EntityType = recommendation.EntityType,
                EntityId = recommendation.EntityId,
                Name = recommendation.Name,
                Description = recommendation.Description,
                Price = recommendation.Price,
                ImageUrl = recommendation.ImageUrl,
                IsPurchasable = true,
                RelevanceScore = recommendation.MatchScore
            };
        }

        private static string BuildHistoryContext(List<ChatbotConversationTurnDto>? history)
        {
            if (history == null || history.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("\n", history
                .Where(h => !string.IsNullOrWhiteSpace(h.Content))
                .Select(h => $"{h.Role}: {h.Content.Trim()}"));
        }

        private static List<ChatbotConversationTurnDto> TrimHistoryToWindow(
            List<ChatbotConversationTurnDto> history,
            int maxTurns,
            int maxInputTokens,
            int reservedOutputTokens)
        {
            if (history.Count == 0)
            {
                return history;
            }

            var effectiveMaxTurns = Math.Max(1, maxTurns);
            var effectiveInputBudget = Math.Max(120, maxInputTokens - Math.Max(0, reservedOutputTokens));
            var selected = new List<ChatbotConversationTurnDto>();
            var usedTokens = 0;

            for (var index = history.Count - 1; index >= 0; index--)
            {
                if (selected.Count >= effectiveMaxTurns)
                {
                    break;
                }

                var turn = history[index];
                if (string.IsNullOrWhiteSpace(turn.Content))
                {
                    continue;
                }

                var availableTokens = effectiveInputBudget - usedTokens;
                if (availableTokens <= 8)
                {
                    break;
                }

                var normalizedContent = turn.Content.Trim();
                var turnTokens = EstimateTokenCount(normalizedContent) + 4;

                if (turnTokens > availableTokens)
                {
                    var truncationBudget = Math.Max(8, availableTokens - 4);
                    normalizedContent = TruncateByApproxTokens(normalizedContent, truncationBudget);
                    turnTokens = EstimateTokenCount(normalizedContent) + 4;
                }

                if (string.IsNullOrWhiteSpace(normalizedContent) || turnTokens > availableTokens)
                {
                    continue;
                }

                selected.Add(new ChatbotConversationTurnDto
                {
                    Role = turn.Role,
                    Content = normalizedContent
                });

                usedTokens += turnTokens;
            }

            selected.Reverse();
            return selected;
        }

        private static int EstimateTokenCount(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            return Math.Max(1, (int)Math.Ceiling(text.Length / 4.0));
        }

        private static string TruncateByApproxTokens(string text, int tokenBudget)
        {
            if (string.IsNullOrWhiteSpace(text) || tokenBudget <= 0)
            {
                return string.Empty;
            }

            var maxChars = Math.Max(1, tokenBudget * 4);
            if (text.Length <= maxChars)
            {
                return text;
            }

            return text[..maxChars].Trim();
        }

        private static int GetPositiveInt(string? value, int fallback)
        {
            return int.TryParse(value, out var parsed) && parsed > 0
                ? parsed
                : fallback;
        }

        private static string NormalizeIntent(string? intent)
        {
            var normalized = intent?.Trim().ToLowerInvariant();
            return normalized switch
            {
                ChatbotIntentPlantSelection => ChatbotIntentPlantSelection,
                ChatbotIntentRoomEnvironment => ChatbotIntentRoomEnvironment,
                ChatbotIntentPlantCare => ChatbotIntentPlantCare,
                ChatbotIntentPolicySupport => ChatbotIntentPolicySupport,
                _ => ChatbotIntentGeneral
            };
        }

        private static int ClampLimit(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }

        private static string ExtractJsonObject(string response)
        {
            var start = response.IndexOf('{');
            var end = response.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                return response;
            }

            return response.Substring(start, end - start + 1);
        }

        private static List<string> NormalizeTextList(List<string>? values)
        {
            if (values == null || values.Count == 0)
            {
                return new List<string>();
            }

            return values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string? FirstNonEmpty(params string?[] values)
        {
            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Trim();
                }
            }

            return null;
        }

        private static List<string>? ResolvePreferredRooms(List<string>? primary, List<string>? fallback)
        {
            var source = primary != null && primary.Any() ? primary : fallback;
            if (source == null)
            {
                return null;
            }

            var normalized = source
                .Where(r => !string.IsNullOrWhiteSpace(r))
                .Select(r => r.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return normalized.Count == 0 ? null : normalized;
        }

        private async Task<List<string>> BuildCareTipsFromPlantGuidesAsync(
            string userMessage,
            string intent,
            List<PlantSuggestionResponseDto> suggestions)
        {
            if (suggestions.Count == 0)
            {
                return new List<string>();
            }

            var mentionedPlants = suggestions
                .Where(s => IsPlantMentionedInMessage(userMessage, s.Name))
                .ToList();

            var targetSuggestions = mentionedPlants.Any()
                ? mentionedPlants
                : (intent == ChatbotIntentPlantCare ? suggestions.Take(2).ToList() : new List<PlantSuggestionResponseDto>());

            if (targetSuggestions.Count == 0)
            {
                return new List<string>();
            }

            var tips = new List<string>();

            foreach (var suggestion in targetSuggestions.Take(2))
            {
                var guide = await GetPlantGuideForSuggestionAsync(suggestion);
                if (guide == null)
                {
                    continue;
                }

                tips.AddRange(ExtractCareTipsFromGuide(guide, suggestion.Name));
            }

            return NormalizeTextList(tips).Take(8).ToList();
        }

        private async Task<DataAccessLayer.Entities.PlantGuide?> GetPlantGuideForSuggestionAsync(PlantSuggestionResponseDto suggestion)
        {
            int? plantId = null;

            switch (suggestion.EntityType)
            {
                case EmbeddingEntityTypes.CommonPlant:
                    var commonPlant = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(suggestion.EntityId);
                    plantId = commonPlant?.PlantId;
                    break;

                case EmbeddingEntityTypes.PlantInstance:
                    var plantInstance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(suggestion.EntityId);
                    plantId = plantInstance?.PlantId;
                    break;
            }

            if (!plantId.HasValue || plantId.Value <= 0)
            {
                return null;
            }

            return await _unitOfWork.PlantGuideRepository.GetByPlantIdWithPlantAsync(plantId.Value);
        }

        private static List<string> ExtractCareTipsFromGuide(DataAccessLayer.Entities.PlantGuide guide, string? fallbackPlantName)
        {
            var plantName = FirstNonEmpty(guide.Plant?.Name, fallbackPlantName, "Cây này") ?? "Cây này";
            var tips = new List<string>();

            if (guide.LightRequirement.HasValue && Enum.IsDefined(typeof(LightRequirementEnum), guide.LightRequirement.Value))
            {
                var lightRequirement = (LightRequirementEnum)guide.LightRequirement.Value;
                tips.Add($"{plantName}: Ánh sáng phù hợp - {MapLightRequirementToVietnamese(lightRequirement)}.");
            }

            if (!string.IsNullOrWhiteSpace(guide.Watering))
            {
                tips.Add($"{plantName}: Tưới nước - {guide.Watering.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(guide.Fertilizing))
            {
                tips.Add($"{plantName}: Bón phân - {guide.Fertilizing.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(guide.Pruning))
            {
                tips.Add($"{plantName}: Cắt tỉa - {guide.Pruning.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(guide.Temperature))
            {
                tips.Add($"{plantName}: Nhiệt độ phù hợp - {guide.Temperature.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(guide.Humidity))
            {
                tips.Add($"{plantName}: Độ ẩm phù hợp - {guide.Humidity.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(guide.Soil))
            {
                tips.Add($"{plantName}: Đất trồng - {guide.Soil.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(guide.CareNotes))
            {
                tips.Add($"{plantName}: Lưu ý chăm sóc - {guide.CareNotes.Trim()}");
            }

            return tips.Take(6).ToList();
        }

        private static bool IsPlantMentionedInMessage(string? message, string? plantName)
        {
            if (string.IsNullOrWhiteSpace(message) || string.IsNullOrWhiteSpace(plantName))
            {
                return false;
            }

            return message.Contains(plantName.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        private static string MapLightRequirementToVietnamese(LightRequirementEnum lightRequirement)
        {
            return lightRequirement switch
            {
                LightRequirementEnum.LowLight => "ánh sáng yếu, phù hợp góc phòng hoặc nơi ít nắng",
                LightRequirementEnum.IndirectLight => "ánh sáng gián tiếp, gần cửa sổ nhưng tránh nắng gắt",
                LightRequirementEnum.PartialSun => "nắng một phần, khoảng 3-6 giờ nắng mỗi ngày",
                LightRequirementEnum.FullSun => "nắng trực tiếp, từ 6 giờ nắng trở lên mỗi ngày",
                _ => "ánh sáng trung bình"
            };
        }

        private static List<string> MergeCareTips(List<string>? primaryTips, List<string>? guideTips)
        {
            var merged = new List<string>();

            if (guideTips != null)
            {
                merged.AddRange(guideTips);
            }

            if (primaryTips != null)
            {
                merged.AddRange(primaryTips);
            }

            return NormalizeTextList(merged).Take(10).ToList();
        }

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

        private sealed class ChatbotIntentAnalysis
        {
            public string? Intent { get; set; }
            public string? SearchQuery { get; set; }
            public string? RoomSummary { get; set; }
            public string? RoomType { get; set; }
            public string? LightingCondition { get; set; }
            public string? FengShuiElement { get; set; }
            public List<string>? PreferredRooms { get; set; }
            public bool? PetSafe { get; set; }
            public bool? ChildSafe { get; set; }
            public decimal? MaxBudget { get; set; }
            public int? RequestedPlantCount { get; set; }
            public List<string>? FollowUpQuestions { get; set; }
        }

        private sealed class ChatbotAnswerPayload
        {
            public string Reply { get; set; } = string.Empty;
            public List<string>? CareTips { get; set; }
            public List<string>? FollowUpQuestions { get; set; }
            public string? Disclaimer { get; set; }
        }

        #endregion
    }
}
