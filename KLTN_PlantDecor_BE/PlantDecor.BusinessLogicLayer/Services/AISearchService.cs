using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Pgvector;
using PlantDecor.BusinessLogicLayer.Constants;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class AISearchService : IAISearchService
    {
        private const string ChatbotIntentPlantSelection = "plant_selection";
        private const string ChatbotIntentRoomEnvironment = "room_environment";
        private const string ChatbotIntentPlantCare = "plant_care";
        private const string ChatbotIntentGeneral = "general";
        private const string ChatbotIntentPolicySupport = "policy_support";
        private const string LanguageEnglish = "en";
        private const string LanguageVietnamese = "vi";
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
                ?? "User policy section on the PlantDecor website/app";
            _returnPolicyDocumentPath = configuration["SupportAndPolicy:ReturnPolicyDocumentPath"]
                ?? "Return policy section on the PlantDecor website/app";

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

        public async Task<PaginatedResult<AIChatSessionListItemResponseDto>> GetAllSessionByUserIdAsync(int userId, Pagination pagination)
        {
            if (userId <= 0)
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }

            var sessions = await _unitOfWork.AIChatSessionRepository
                .GetUserSessionsAsync(userId, pagination.PageNumber, pagination.PageSize);
            var totalCount = await _unitOfWork.AIChatSessionRepository
                .GetUserSessionsCountAsync(userId);

            return new PaginatedResult<AIChatSessionListItemResponseDto>(
                sessions.ToSessionListItemResponses(),
                totalCount,
                pagination.PageNumber,
                pagination.PageSize);
        }

        public async Task<AIChatConversationHistoryResponseDto> GetConversationHistoryBySessionIdAsync(int userId, int sessionId, Pagination pagination)
        {
            if (userId <= 0)
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }

            if (sessionId <= 0)
            {
                throw new BadRequestException("SessionId must be greater than 0.");
            }

            var session = await _unitOfWork.AIChatSessionRepository.GetByIdAndUserAsync(sessionId, userId);
            if (session == null)
            {
                throw new NotFoundException("AI chat session not found.");
            }

            var messages = await _unitOfWork.AIChatMessageRepository
                .GetSessionMessagesAsync(sessionId, userId, pagination.PageNumber, pagination.PageSize);
            var totalCount = await _unitOfWork.AIChatMessageRepository
                .GetSessionMessagesCountAsync(sessionId, userId);

            return new AIChatConversationHistoryResponseDto
            {
                SessionId = session.Id,
                Title = session.Title,
                Status = session.Status == (int)AIChatSessionStatusEnum.Closed ? "closed" : "active",
                StartedAt = session.StartedAt,
                EndedAt = session.EndedAt,
                TotalCount = totalCount,
                PageNumber = pagination.PageNumber,
                PageSize = pagination.PageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pagination.PageSize),
                Messages = messages.ToMessageHistoryItemResponses()
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
                    var validationFallbackLanguage = ResolveResponseLanguage(userMessage, request.RoomDescription, null);
                    var validationFallback = ClampChatbotResponsePayload(BuildFallbackChatbotResponse(
                        ChatbotIntentGeneral,
                        new List<PlantSuggestionResponseDto>(),
                        null,
                        validationFallbackLanguage == LanguageVietnamese
                            ? "Ban hay mo ta nhu cau chon cay, moi truong phong hoac van de cham soc de minh tu van chinh xac hon nhe."
                            : "Please describe your plant preferences, room conditions, or care issue so I can give you more accurate advice.",
                        null,
                        validationFallbackLanguage));
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

                var responseLanguage = ResolveResponseLanguage(userMessage, request.RoomDescription, boundedHistory);

                var intentAnalysis = await AnalyzeChatIntentAsync(request, boundedHistory);
                var intent = NormalizeIntent(intentAnalysis.Intent);

                if (intent == ChatbotIntentPolicySupport)
                {
                    var policyResponse = ClampChatbotResponsePayload(await BuildPolicySupportResponseAsync(responseLanguage));
                    _logger.LogInformation(
                        "AI policy branch routed. SessionId={SessionId}, UserId={UserId}, PolicySources={PolicySources}, Language={Language}",
                        sessionId,
                        userId,
                        policyResponse.PolicySources.Count,
                        responseLanguage);
                    await PersistAssistantMessageAsync(sessionId, userId, policyResponse, ChatbotIntentPolicySupport, false, true);
                    return policyResponse;
                }

                var requestedFengShuiElement = ToEnumName(request.FengShuiElement);
                var requestedPreferredRooms = ToEnumNames(request.PreferredRooms);

                var effectiveLimit = ClampLimit(request.Limit ?? intentAnalysis.RequestedPlantCount ?? 5, 1, 10);
                var effectiveFengShuiElement = FirstNonEmpty(requestedFengShuiElement, intentAnalysis.FengShuiElement);
                var effectivePreferredRooms = ResolvePreferredRooms(requestedPreferredRooms, intentAnalysis.PreferredRooms);
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
                plantGuideCareTips = LocalizeCareTips(plantGuideCareTips, responseLanguage);

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
                    effectiveChildSafe,
                    responseLanguage);

                if (answer == null)
                {
                    var llmFallback = ClampChatbotResponsePayload(BuildFallbackChatbotResponse(intent, suggestions, roomSummary, null, plantGuideCareTips, responseLanguage));
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
                    ResolveResponseLanguage(userMessage, request.RoomDescription, null) == LanguageVietnamese
                        ? "Hien he thong AI dang ban, ban thu lai sau it phut hoac gui mo ta ngan gon hon."
                        : "The AI service is currently busy. Please try again in a few minutes or send a shorter description.",
                    null,
                    ResolveResponseLanguage(userMessage, request.RoomDescription, null)));
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
            var requestedFengShuiElement = ToEnumName(request.FengShuiElement);
            var requestedPreferredRooms = ToEnumNames(request.PreferredRooms);

            var fallback = new ChatbotIntentAnalysis
            {
                Intent = ChatbotIntentGeneral,
                SearchQuery = request.Message ?? string.Empty,
                RoomSummary = request.RoomDescription ?? string.Empty,
                FengShuiElement = requestedFengShuiElement,
                PreferredRooms = requestedPreferredRooms,
                PetSafe = request.PetSafe,
                ChildSafe = request.ChildSafe,
                MaxBudget = request.MaxBudget,
                RequestedPlantCount = request.Limit
            };

            try
            {
                var historyContext = BuildHistoryContext(conversationHistory);

                var parsePrompt =
                    "You are an intent parser for a plant recommendation chatbot. " +
                    "Return ONLY one valid JSON object with fields: " +
                    "intent (plant_selection|room_environment|plant_care|policy_support|general), " +
                    "searchQuery, roomSummary, roomType, lightingCondition, fengShuiElement, " +
                    "preferredRooms (array of string), petSafe (bool|null), childSafe (bool|null), " +
                    "maxBudget (number|null), requestedPlantCount (int|null), followUpQuestions (array of string). " +
                    "If uncertain, use null or an empty array. Do not include markdown.";

                var payload = JsonSerializer.Serialize(new
                {
                    message = request.Message,
                    roomDescription = request.RoomDescription,
                    fengShuiElement = requestedFengShuiElement,
                    maxBudget = request.MaxBudget,
                    preferredRooms = requestedPreferredRooms,
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
                parsed.FengShuiElement = FirstNonEmpty(parsed.FengShuiElement, requestedFengShuiElement);
                parsed.PreferredRooms = ResolvePreferredRooms(parsed.PreferredRooms, requestedPreferredRooms);
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
            bool? childSafe,
            string responseLanguage)
        {
            try
            {
                var outputLanguageName = responseLanguage == LanguageVietnamese ? "Vietnamese" : "English";
                var answerPrompt =
                    "You are the PlantDecor AI chatbot. Tasks: plant selection guidance, basic room-environment understanding, and plant care support. " +
                    "Be practical, user-friendly, and do not invent products outside provided data. " +
                    "Reply in " + outputLanguageName + ". " +
                    "Return ONLY one JSON object with fields: reply (string), careTips (array string), " +
                    "followUpQuestions (array string), disclaimer (string|null). " +
                    "If careTips or followUpQuestions is unavailable, return an empty array.";

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
            List<string>? plantGuideCareTips = null,
            string responseLanguage = LanguageEnglish)
        {
            var isVietnamese = responseLanguage == LanguageVietnamese;
            var suggestionText = suggestions.Any()
                ? (isVietnamese
                    ? $"Minh co {suggestions.Count} goi y phu hop, ban co the xem danh sach cay ben duoi de chon nhanh."
                    : $"I found {suggestions.Count} matching suggestions. You can review the plant list below for a quick choice.")
                : (isVietnamese
                    ? "Minh chua tim thay cay phu hop ngay bay gio, ban thu them thong tin ve anh sang, ngan sach hoac nhu cau cham soc nhe."
                    : "I could not find a strong match yet. Try adding details about lighting, budget, or care preference.");

            var defaultCareTips = isVietnamese
                ? new List<string>
                {
                    "Kiem tra do am dat truoc khi tuoi, tranh tuoi theo lich co dinh.",
                    "Dat cay o noi co anh sang phu hop voi tung loai cay, tranh nang gat truc tiep ca ngay.",
                    "Quan sat la vang hoac ung de dieu chinh luong nuoc va thong gio."
                }
                : new List<string>
                {
                    "Check soil moisture before watering and avoid fixed watering schedules.",
                    "Place each plant in suitable light and avoid harsh direct sun all day.",
                    "Watch for yellowing leaves or root rot signs to adjust watering and airflow."
                };

            return new AIChatbotResponseDto
            {
                Intent = NormalizeIntent(intent),
                Reply = fallbackReply ?? suggestionText,
                RoomEnvironmentSummary = roomSummary,
                SuggestedPlants = suggestions,
                CareTips = MergeCareTips(defaultCareTips, plantGuideCareTips),
                FollowUpQuestions = isVietnamese
                    ? new List<string>
                    {
                        "Phong cua ban nhan anh sang truc tiep hay anh sang tan xa?",
                        "Ban uu tien cay de cham hay cay co gia tri tham my cao?"
                    }
                    : new List<string>
                    {
                        "Does your room get direct sunlight or mostly indirect light?",
                        "Do you prefer low-maintenance plants or stronger decorative impact?"
                    },
                PolicySources = new List<PolicyGroundingSourceDto>(),
                Disclaimer = isVietnamese
                    ? "Thong tin chi mang tinh tham khao. Neu cay co dau hieu benh nang, ban nen lien he chuyen gia cham soc cay."
                    : "This information is for reference only. If your plant shows severe disease symptoms, please contact a plant care specialist.",
                UsedFallback = true
            };
        }

        private async Task<AIChatbotResponseDto> BuildPolicySupportResponseAsync(string responseLanguage)
        {
            try
            {
                var isVietnamese = responseLanguage == LanguageVietnamese;
                var userPolicies = await _policyKnowledgeService.GetByCategoryActiveAsync(PolicyContentCategoryEnum.UserPolicy);
                var returnPolicies = await _policyKnowledgeService.GetByCategoryActiveAsync(PolicyContentCategoryEnum.ReturnPolicy);
                var policySources = BuildPolicyGroundingSources(userPolicies, returnPolicies);

                var userPolicySection = BuildPolicySection(
                    isVietnamese ? "Chinh sach nguoi dung" : "User Policy",
                    userPolicies,
                    responseLanguage);
                var returnPolicySection = BuildPolicySection(
                    isVietnamese ? "Chinh sach hoan tra" : "Return Policy",
                    returnPolicies,
                    responseLanguage);

                if (string.IsNullOrWhiteSpace(userPolicySection) && string.IsNullOrWhiteSpace(returnPolicySection))
                {
                    return BuildPolicySupportFallbackResponse(responseLanguage);
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
                        (isVietnamese
                            ? "Voi cau hoi ve chinh sach nguoi dung hoac hoan tra, minh da lay thong tin tu noi dung chinh sach dang active trong he thong:\n"
                            : "For user-policy or return-policy questions, I retrieved information from active policy content in the system:\n") +
                        groundedSummary + "\n" +
                        (isVietnamese
                            ? "De dam bao thong tin cuoi cung chinh xac tai thoi diem giao dich, ban vui long chat voi tu van vien. "
                            : "To ensure final accuracy at transaction time, please confirm with a support consultant. ") +
                        (isVietnamese
                            ? $"Ban cung co the xem chinh sach tai {_userPolicyDocumentPath} va {_returnPolicyDocumentPath}."
                            : $"You can also review policy documents at {_userPolicyDocumentPath} and {_returnPolicyDocumentPath}."),
                    SuggestedPlants = new List<PlantSuggestionResponseDto>(),
                    CareTips = new List<string>(),
                    FollowUpQuestions = isVietnamese
                        ? new List<string>
                        {
                            "Ban can minh trich ngan hon phan chinh sach nguoi dung hay chinh sach hoan tra?",
                            "Ban muon minh nhac lai la nen chat voi tu van vien de xac nhan phien ban chinh sach moi nhat khong?"
                        }
                        : new List<string>
                        {
                            "Do you want a shorter summary of the user policy or the return policy?",
                            "Would you like me to remind you to confirm the latest policy version with a support consultant?"
                        },
                    PolicySources = policySources,
                    Disclaimer = isVietnamese
                        ? "Noi dung chinh sach co the thay doi theo thoi diem, vui long xac nhan voi tu van vien truoc khi thuc hien giao dich."
                        : "Policy content can change over time. Please confirm with a support consultant before any transaction.",
                    UsedFallback = false
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to build DB-grounded policy response. Falling back to safe support response.");
                return BuildPolicySupportFallbackResponse(responseLanguage);
            }
        }

        private AIChatbotResponseDto BuildPolicySupportFallbackResponse(string responseLanguage)
        {
            var isVietnamese = responseLanguage == LanguageVietnamese;
            return new AIChatbotResponseDto
            {
                Intent = ChatbotIntentPolicySupport,
                Reply =
                    (isVietnamese
                        ? "Voi cau hoi ve chinh sach nguoi dung hoac hoan tra, minh can chuyen ban sang kenh ho tro nguoi that de dam bao thong tin chinh xac va cap nhat. "
                        : "For user-policy or return-policy questions, I need to route you to human support to ensure accurate and up-to-date information. ") +
                    (isVietnamese
                        ? $"Ban vui long chat truc tiep voi tu van vien trong muc Ho tro, hoac xem file/chuyen muc chinh sach tai {_userPolicyDocumentPath} va {_returnPolicyDocumentPath}."
                        : $"Please chat directly with a support consultant, or review policy documents at {_userPolicyDocumentPath} and {_returnPolicyDocumentPath}."),
                SuggestedPlants = new List<PlantSuggestionResponseDto>(),
                CareTips = new List<string>(),
                FollowUpQuestions = isVietnamese
                    ? new List<string>
                    {
                        "Ban muon minh nhac lai la hay vao muc Ho tro de chat voi tu van vien khong?",
                        "Ban dang can chinh sach nguoi dung hay chinh sach hoan tra?"
                    }
                    : new List<string>
                    {
                        "Would you like me to remind you to open Support and chat with a consultant?",
                        "Are you looking for the user policy or the return policy?"
                    },
                PolicySources = new List<PolicyGroundingSourceDto>(),
                Disclaimer = isVietnamese
                    ? "Noi dung chinh sach co the thay doi theo thoi diem, vui long xac nhan voi tu van vien truoc khi thuc hien giao dich."
                    : "Policy content can change over time. Please confirm with a support consultant before any transaction.",
                UsedFallback = false
            };
        }

        private static string BuildPolicySection(string sectionTitle, List<DataAccessLayer.Entities.PolicyContent>? policies, string responseLanguage)
        {
            if (policies == null || policies.Count == 0)
            {
                return string.Empty;
            }

            var isVietnamese = responseLanguage == LanguageVietnamese;

            var excerpts = policies
                .Where(p => p.IsActive == true && !string.IsNullOrWhiteSpace(p.Content))
                .OrderBy(p => p.DisplayOrder ?? int.MaxValue)
                .ThenBy(p => p.Id)
                .Take(2)
                .Select(p =>
                {
                    var title = FirstNonEmpty(p.Title, sectionTitle, isVietnamese ? "Chinh sach" : "Policy")
                        ?? (isVietnamese ? "Chinh sach" : "Policy");
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
            var serializerOptions = new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            await _unitOfWork.AIChatMessageRepository.AddAssistantMessageAsync(
                sessionId,
                userId,
                response.Reply,
                intent,
                isFallback,
                isPolicyResponse,
                suggestedPlants: response.SuggestedPlants == null || response.SuggestedPlants.Count == 0
                    ? null
                    : JsonSerializer.Serialize(response.SuggestedPlants, serializerOptions),
                careTips: response.CareTips == null || response.CareTips.Count == 0
                    ? null
                    : JsonSerializer.Serialize(response.CareTips, serializerOptions));

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

        private static string? ToEnumName(FengShuiElementTypeEnum? value)
        {
            return value?.ToString();
        }

        private static List<string>? ToEnumNames(List<RoomTypeEnum>? values)
        {
            if (values == null || values.Count == 0)
            {
                return null;
            }

            var normalized = values
                .Select(v => v.ToString())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            return normalized.Count == 0 ? null : normalized;
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

        private static List<string> LocalizeCareTips(List<string> tips, string responseLanguage)
        {
            if (tips == null || tips.Count == 0)
            {
                return new List<string>();
            }

            if (responseLanguage == LanguageVietnamese)
            {
                return tips
                    .Select(LocalizeCareTipToVietnamese)
                    .ToList();
            }

            return tips
                .Select(LocalizeCareTipToEnglish)
                .ToList();
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
            var plantName = FirstNonEmpty(guide.Plant?.Name, fallbackPlantName, "This plant") ?? "This plant";
            var tips = new List<string>();

            if (guide.LightRequirement.HasValue && Enum.IsDefined(typeof(LightRequirementEnum), guide.LightRequirement.Value))
            {
                var lightRequirement = (LightRequirementEnum)guide.LightRequirement.Value;
                tips.Add($"{plantName}: Suitable light - {MapLightRequirementToText(lightRequirement, LanguageEnglish)}.");
            }

            if (!string.IsNullOrWhiteSpace(guide.Watering))
            {
                tips.Add($"{plantName}: Watering - {guide.Watering.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(guide.Fertilizing))
            {
                tips.Add($"{plantName}: Fertilizing - {guide.Fertilizing.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(guide.Pruning))
            {
                tips.Add($"{plantName}: Pruning - {guide.Pruning.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(guide.Temperature))
            {
                tips.Add($"{plantName}: Suitable temperature - {guide.Temperature.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(guide.Humidity))
            {
                tips.Add($"{plantName}: Suitable humidity - {guide.Humidity.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(guide.Soil))
            {
                tips.Add($"{plantName}: Soil - {guide.Soil.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(guide.CareNotes))
            {
                tips.Add($"{plantName}: Care notes - {guide.CareNotes.Trim()}");
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

        private static string MapLightRequirementToText(LightRequirementEnum lightRequirement, string responseLanguage)
        {
            var isVietnamese = responseLanguage == LanguageVietnamese;
            return lightRequirement switch
            {
                LightRequirementEnum.LowLight => isVietnamese
                    ? "anh sang yeu, phu hop goc phong hoac noi it nang"
                    : "low light, suitable for corners or low-sun spaces",
                LightRequirementEnum.IndirectLight => isVietnamese
                    ? "anh sang gian tiep, gan cua so nhung tranh nang gat"
                    : "indirect light, near windows but away from harsh sun",
                LightRequirementEnum.PartialSun => isVietnamese
                    ? "nang mot phan, khoang 3-6 gio nang moi ngay"
                    : "partial sun, around 3-6 hours of sunlight daily",
                LightRequirementEnum.FullSun => isVietnamese
                    ? "nang truc tiep, tu 6 gio nang tro len moi ngay"
                    : "full sun, at least 6 hours of direct sunlight daily",
                _ => isVietnamese ? "anh sang trung binh" : "moderate light"
            };
        }

        private static string LocalizeCareTipToVietnamese(string tip)
        {
            return tip
                .Replace("Suitable light", "Anh sang phu hop", StringComparison.OrdinalIgnoreCase)
                .Replace("Watering", "Tuoi nuoc", StringComparison.OrdinalIgnoreCase)
                .Replace("Fertilizing", "Bon phan", StringComparison.OrdinalIgnoreCase)
                .Replace("Pruning", "Cat tia", StringComparison.OrdinalIgnoreCase)
                .Replace("Suitable temperature", "Nhiet do phu hop", StringComparison.OrdinalIgnoreCase)
                .Replace("Suitable humidity", "Do am phu hop", StringComparison.OrdinalIgnoreCase)
                .Replace("Soil", "Dat trong", StringComparison.OrdinalIgnoreCase)
                .Replace("Care notes", "Luu y cham soc", StringComparison.OrdinalIgnoreCase)
                .Replace("This plant", "Cay nay", StringComparison.OrdinalIgnoreCase);
        }

        private static string LocalizeCareTipToEnglish(string tip)
        {
            return tip
                .Replace("Anh sang phu hop", "Suitable light", StringComparison.OrdinalIgnoreCase)
                .Replace("Tuoi nuoc", "Watering", StringComparison.OrdinalIgnoreCase)
                .Replace("Bon phan", "Fertilizing", StringComparison.OrdinalIgnoreCase)
                .Replace("Cat tia", "Pruning", StringComparison.OrdinalIgnoreCase)
                .Replace("Nhiet do phu hop", "Suitable temperature", StringComparison.OrdinalIgnoreCase)
                .Replace("Do am phu hop", "Suitable humidity", StringComparison.OrdinalIgnoreCase)
                .Replace("Dat trong", "Soil", StringComparison.OrdinalIgnoreCase)
                .Replace("Luu y cham soc", "Care notes", StringComparison.OrdinalIgnoreCase)
                .Replace("Cay nay", "This plant", StringComparison.OrdinalIgnoreCase);
        }

        private static string ResolveResponseLanguage(
            string? userMessage,
            string? roomDescription,
            List<ChatbotConversationTurnDto>? history)
        {
            var primaryText = FirstNonEmpty(userMessage, roomDescription);
            var detectedPrimary = DetectLanguageCode(primaryText);
            if (!string.IsNullOrWhiteSpace(detectedPrimary))
            {
                return detectedPrimary;
            }

            if (history != null)
            {
                var recentUserText = history
                    .AsEnumerable()
                    .Reverse()
                    .FirstOrDefault(h => string.Equals(h.Role, "user", StringComparison.OrdinalIgnoreCase)
                        && !string.IsNullOrWhiteSpace(h.Content))
                    ?.Content;

                var detectedFromHistory = DetectLanguageCode(recentUserText);
                if (!string.IsNullOrWhiteSpace(detectedFromHistory))
                {
                    return detectedFromHistory;
                }
            }

            return LanguageEnglish;
        }

        private static string DetectLanguageCode(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return LanguageEnglish;
            }

            var normalized = text.Trim();
            var hasVietnameseChars = normalized.Any(c => "ăâđêôơưáàảãạắằẳẵặấầẩẫậéèẻẽẹếềểễệíìỉĩịóòỏõọốồổỗộớờởỡợúùủũụứừửữựýỳỷỹỵ".Contains(char.ToLowerInvariant(c)));
            if (hasVietnameseChars)
            {
                return LanguageVietnamese;
            }

            var lower = normalized.ToLowerInvariant();
            var vietnameseHints = new[]
            {
                " toi ", " ban ", " cay ", " phong ", " cham soc ", " tu van ", " chinh sach ", " hoan tra ", " anh sang ", " ngan sach "
            };

            if (vietnameseHints.Any(h => lower.Contains(h.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return LanguageVietnamese;
            }

            return LanguageEnglish;
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
