using Microsoft.EntityFrameworkCore;
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
        private readonly ICareServicePackageService _careServicePackageService;
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
            ICareServicePackageService careServicePackageService,
            IConfiguration configuration,
            ILogger<AISearchService> logger)
        {
            _unitOfWork = unitOfWork;
            _azureOpenAIService = azureOpenAIService;
            _policyKnowledgeService = policyKnowledgeService;
            _careServicePackageService = careServicePackageService;
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
                var embeddings = await _unitOfWork.EmbeddingRepository.SearchSimilarAsync(vector, searchLimit, entityTypes);

                // 3. Filter by purchasable status and enrich results
                var results = new List<SearchResultItemDto>();

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
                    var item = await EnrichSearchResultAsync(embedding, originalEntityId, isPurchasable);
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
                        PlantId = item.PlantId,
                        PlantComboId = item.PlantComboId,
                        MaterialId = item.MaterialId,
                        Name = item.Name,
                        Description = item.Description,
                        Price = item.Price,
                        ImageUrl = item.ImageUrl,
                        FengShuiElement = item.FengShuiElement,
                        MatchScore = item.SimilarityScore,
                        NurseryId = item.NurseryId,
                        NurseryName = item.NurseryName,
                        NurseryAddress = item.NurseryAddress,
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
                        PlantId = r.PlantId,
                        PlantComboId = r.PlantComboId,
                        MaterialId = r.MaterialId,
                        Name = r.Name,
                        Description = r.Description,
                        Price = r.Price,
                        ImageUrl = r.ImageUrl,
                        IsPurchasable = r.IsPurchasable,
                        RelevanceScore = r.SimilarityScore,
                        NurseryId = r.NurseryId > 0 ? r.NurseryId : null,
                        NurseryName = r.NurseryName,
                        NurseryAddress = r.NurseryAddress
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

        public async Task CloseChatSessionAsync(int userId, int sessionId)
        {
            if (userId <= 0)
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }

            if (sessionId <= 0)
            {
                throw new BadRequestException("SessionId must be greater than 0.");
            }

            var closed = await _unitOfWork.AIChatSessionRepository.CloseSessionAsync(sessionId, userId);
            if (!closed)
            {
                throw new NotFoundException("AI chat session not found.");
            }
        }

        public async Task<AIChatSessionResponseDto> RenameChatSessionAsync(int userId, int sessionId, string? title)
        {
            if (userId <= 0)
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }

            if (sessionId <= 0)
            {
                throw new BadRequestException("SessionId must be greater than 0.");
            }

            var session = await _unitOfWork.AIChatSessionRepository.UpdateTitleAsync(sessionId, userId, title);
            if (session == null)
            {
                throw new NotFoundException("AI chat session not found.");
            }

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
                    userMessage,
                    intentAnalysis.SearchQuery,
                    "Cây cảnh trong nhà dễ chăm sóc")
                    ?? "Cây cảnh trong nhà dễ chăm sóc";

                var recommendedCareServicePackages = await BuildRecommendedCareServicePackagesAsync(userId, request.OrderId);
                var suggestedCareServicePackages = MapCareServicePackageSuggestions(recommendedCareServicePackages, 8);
                var careServicePackageHeaderText = BuildCareServicePackageHeaderText(suggestedCareServicePackages, responseLanguage);

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

                var authoritativeFacts = await BuildAuthoritativeFactsForSuggestionsAsync(suggestions, responseLanguage);
                ApplyAuthoritativeFactsToSuggestions(suggestions, authoritativeFacts);
                if (request.OnlyPurchasable)
                {
                    suggestions = suggestions
                        .Where(s => authoritativeFacts.Any(f =>
                            f.EntityType == s.EntityType
                            && f.EntityId == s.EntityId
                            && f.IsPurchasable))
                        .ToList();
                    authoritativeFacts = authoritativeFacts
                        .Where(f => suggestions.Any(s => s.EntityType == f.EntityType && s.EntityId == f.EntityId))
                        .ToList();
                }

                var plantGuideCareTips = await BuildCareTipsFromPlantGuidesAsync(userMessage, intent, suggestions);
                plantGuideCareTips = LocalizeCareTips(plantGuideCareTips, responseLanguage);

                var answer = await GenerateChatbotAnswerAsync(
                    request,
                    intent,
                    roomSummary,
                    suggestions,
                    authoritativeFacts,
                    plantGuideCareTips,
                    intentAnalysis,
                    boundedHistory,
                    effectiveFengShuiElement,
                    effectivePreferredRooms,
                    effectiveMaxBudget,
                    effectivePetSafe,
                    effectiveChildSafe,
                    recommendedCareServicePackages,
                    careServicePackageHeaderText,
                    responseLanguage);

                if (answer == null)
                {
                    var llmFallback = ClampChatbotResponsePayload(BuildFallbackChatbotResponse(intent, suggestions, roomSummary, null, plantGuideCareTips, responseLanguage));
                    await PersistAssistantMessageAsync(sessionId, userId, llmFallback, intent, true, false);
                    return llmFallback;
                }

                var quickReplyPrompts = BuildUserPromptSuggestions(intent, suggestions, authoritativeFacts, responseLanguage);
                var response = ClampChatbotResponsePayload(new AIChatbotResponseDto
                {
                    Intent = intent,
                    Reply = PrependCareServicePackageHeaderIfNeeded(
                        careServicePackageHeaderText,
                        RemoveLeadingCareServicePackageEcho(answer.Reply, suggestedCareServicePackages, responseLanguage)),
                    RoomEnvironmentSummary = roomSummary,
                    SuggestedPlants = suggestions,
                    SuggestedCareServicePackages = suggestedCareServicePackages,
                    CareTips = MergeCareTips(answer.CareTips, plantGuideCareTips),
                    FollowUpQuestions = quickReplyPrompts,
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

        private static List<CareServicePackageSuggestionDto> MapCareServicePackageSuggestions(
            List<CareServicePackageRecommendationResponseDto> recommendations,
            int limit)
        {
            var normalizedLimit = Math.Clamp(limit, 1, 10);
            var items = recommendations ?? new List<CareServicePackageRecommendationResponseDto>();

            return items
                .OrderByDescending(r => r.MatchScore)
                .ThenBy(r => r.UnitPrice ?? decimal.MaxValue)
                .Take(normalizedLimit)
                .Select(r => new CareServicePackageSuggestionDto
                {
                    PackageId = r.PackageId,
                    PackageName = r.PackageName,
                    UnitPrice = r.UnitPrice,
                    MatchScore = r.MatchScore,
                    MatchReasons = r.MatchReasons?.ToList() ?? new List<string>()
                })
                .ToList();
        }

        private static string? BuildCareServicePackageHeaderText(
            List<CareServicePackageSuggestionDto> suggestions,
            string responseLanguage)
        {
            if (suggestions == null || suggestions.Count == 0)
                return null;

            var isVietnamese = responseLanguage == LanguageVietnamese;
            var lines = new List<string>
            {
                isVietnamese
                    ? "Dua tren cay trong don hang, minh goi y cac goi cham soc phu hop nhat:"
                    : "Based on plants in your order, here are the best matching care service packages:"
            };

            for (var i = 0; i < suggestions.Count; i++)
            {
                var s = suggestions[i];
                lines.Add(s.PackageName);
            }

            return string.Join("\n", lines).Trim();
        }

        private static string PrependCareServicePackageHeaderIfNeeded(string? headerText, string reply)
        {
            if (string.IsNullOrWhiteSpace(headerText))
                return reply;

            var trimmedReply = reply?.Trim() ?? string.Empty;
            if (trimmedReply.StartsWith(headerText, StringComparison.OrdinalIgnoreCase))
                return trimmedReply;

            return string.IsNullOrWhiteSpace(trimmedReply)
                ? headerText.Trim()
                : $"{headerText.Trim()}\n\n{trimmedReply}";
        }

        private static string RemoveLeadingCareServicePackageEcho(
            string reply,
            List<CareServicePackageSuggestionDto> suggestions,
            string responseLanguage)
        {
            var text = reply?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(text) || suggestions == null || suggestions.Count == 0)
                return text;

            var isVietnamese = responseLanguage == LanguageVietnamese;
            var patterns = isVietnamese
                ? new[]
                {
                    "Dua tren cay trong don hang",
                    "Dua tren don hang",
                    "Minh goi y cac goi cham soc",
                }
                : new[]
                {
                    "Based on plants in your order",
                    "Based on your order",
                    "Here are the best matching care service packages",
                };

            foreach (var p in patterns)
            {
                if (!text.StartsWith(p, StringComparison.OrdinalIgnoreCase))
                    continue;

                var idx = text.IndexOf('\n');
                text = idx >= 0 ? text[(idx + 1)..].Trim() : string.Empty;
                break;
            }

            var firstName = suggestions[0].PackageName?.Trim();
            if (!string.IsNullOrWhiteSpace(firstName)
                && text.StartsWith(firstName, StringComparison.OrdinalIgnoreCase))
            {
                var idx = text.IndexOf('\n');
                text = idx >= 0 ? text[(idx + 1)..].Trim() : string.Empty;
            }

            // If the LLM repeats the package in the first sentence (single-line output),
            // strip that first sentence to reduce perceived duplication.
            if (!string.IsNullOrWhiteSpace(firstName) && !string.IsNullOrWhiteSpace(text))
            {
                var leading = text.Length > 240 ? text[..240] : text;
                var startsWithEcho = isVietnamese
                    ? leading.StartsWith("Voi don hang", StringComparison.OrdinalIgnoreCase)
                      || leading.StartsWith("Trong don hang", StringComparison.OrdinalIgnoreCase)
                      || leading.StartsWith("Goi phu hop nhat", StringComparison.OrdinalIgnoreCase)
                    : leading.StartsWith("For your order", StringComparison.OrdinalIgnoreCase)
                      || leading.StartsWith("In your order", StringComparison.OrdinalIgnoreCase)
                      || leading.StartsWith("The best", StringComparison.OrdinalIgnoreCase);

                if (startsWithEcho && leading.Contains(firstName, StringComparison.OrdinalIgnoreCase))
                {
                    var endIdx = text.IndexOfAny(new[] { '.', '!', '?' });
                    if (endIdx >= 0 && endIdx + 1 < text.Length)
                    {
                        text = text[(endIdx + 1)..].Trim();
                    }
                    else
                    {
                        // Fallback: drop the whole line if no sentence terminator.
                        var nlIdx = text.IndexOf('\n');
                        text = nlIdx >= 0 ? text[(nlIdx + 1)..].Trim() : string.Empty;
                    }
                }
            }

            return text;
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
            List<AuthoritativeEntityFactDto> authoritativeFacts,
            List<string> authoritativeCareTips,
            ChatbotIntentAnalysis intentAnalysis,
            List<ChatbotConversationTurnDto> conversationHistory,
            string? fengShuiElement,
            List<string>? preferredRooms,
            decimal? maxBudget,
            bool? petSafe,
            bool? childSafe,
            List<CareServicePackageRecommendationResponseDto> recommendedCareServicePackages,
            string? careServicePackageHeaderText,
            string responseLanguage)
        {
            try
            {
                var outputLanguageName = responseLanguage == LanguageVietnamese ? "Vietnamese" : "English";
                var answerPrompt =
                    "You are the PlantDecor AI chatbot. Tasks: plant selection guidance, basic room-environment understanding, and plant care support. " +
                    "Be practical, user-friendly, and do not invent products outside provided data. " +
                    "If careServicePackageHeaderText is provided, it is the authoritative package shortlist. Do NOT say there are no suitable packages and do NOT contradict it. " +
                    "Do NOT repeat careServicePackageHeaderText verbatim in your reply. Start with practical advice and how to choose among the listed packages. " +
                    "If recommendedCareServicePackages is provided, prioritize advising packages from that list for care service consultation and do not invent other packages. " +
                    "The authoritativeFacts and authoritativeCareTips in the payload are the latest database state. " +
                    "If they conflict with history, suggestedPlants, embedding/search text, or earlier assistant answers, trust authoritativeFacts. " +
                    "Do not recommend an item as purchasable when authoritativeFacts marks it not purchasable, inactive, sold, expired, or out of stock. " +
                    "Reply in " + outputLanguageName + ". " +
                    "Return ONLY one JSON object with fields: reply (string), careTips (array string), disclaimer (string|null). " +
                    "If careTips is unavailable, return an empty array.";

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
                        s.PlantId,
                        s.PlantComboId,
                        s.MaterialId,
                        s.Name,
                        s.Description,
                        s.Price,
                        s.NurseryId,
                        s.NurseryName,
                        s.NurseryAddress,
                        s.IsPurchasable,
                        s.RelevanceScore
                    }).ToList(),
                    authoritativeFacts = authoritativeFacts.Select(f => new
                    {
                        f.EntityType,
                        f.EntityId,
                        f.Name,
                        f.Description,
                        f.Price,
                        f.PlantId,
                        f.PlantComboId,
                        f.MaterialId,
                        f.NurseryId,
                        f.NurseryName,
                        f.NurseryAddress,
                        f.IsActive,
                        f.IsPurchasable,
                        f.AvailabilityStatus,
                        f.Quantity,
                        f.StatusName,
                        f.ExpiredDate,
                        f.Categories,
                        f.Tags,
                        f.RoomTypes,
                        f.RoomStyles,
                        f.FengShuiElement,
                        f.FengShuiMeaning,
                        f.PetSafe,
                        f.ChildSafe,
                        f.AirPurifying,
                        f.Brand,
                        f.Unit,
                        f.Specifications,
                        f.SuitableSpace,
                        f.SuitableRooms,
                        f.Season,
                        f.LightRequirementName,
                        f.CareTips
                    }).ToList(),
                    authoritativeCareTips,
                    careServicePackageHeaderText = string.IsNullOrWhiteSpace(careServicePackageHeaderText) ? null : careServicePackageHeaderText,
                    recommendedCareServicePackages = (recommendedCareServicePackages ?? new List<CareServicePackageRecommendationResponseDto>())
                        .OrderByDescending(p => p.MatchScore)
                        .ThenBy(p => p.UnitPrice ?? decimal.MaxValue)
                        .Take(5)
                        .Select(p => new
                        {
                            p.PackageId,
                            p.PackageName,
                            p.UnitPrice,
                            p.MatchScore,
                            p.MatchedCategoryCount,
                            p.MatchedCareLevelCount,
                            p.TotalPurchasedPlantItems,
                            p.MatchReasons,
                            Plants = p.Plants.Select(pl => new { pl.PlantId, pl.PlantName, pl.Quantity }).ToList()
                        })
                        .ToList(),
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

        private async Task<List<CareServicePackageRecommendationResponseDto>> BuildRecommendedCareServicePackagesAsync(int userId, int? orderId)
        {
            if (!orderId.HasValue || orderId.Value <= 0)
                return new List<CareServicePackageRecommendationResponseDto>();

            try
            {
                var user = await _unitOfWork.UserRepository.GetByIdAsync(userId);
                if (user == null || user.RoleId != (int)RoleEnum.Customer)
                {
                    return new List<CareServicePackageRecommendationResponseDto>();
                }

                return await _careServicePackageService.RecommendByOrderForCustomerAsync(userId, orderId.Value);
            }
            catch (NotFoundException ex)
            {
                // Avoid breaking chatbot when suitability mapping is incomplete.
                _logger.LogWarning(ex, "Unable to build care service package recommendations for OrderId={OrderId}", orderId.Value);
                return new List<CareServicePackageRecommendationResponseDto>();
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
                FollowUpQuestions = BuildUserPromptSuggestions(intent, suggestions, new List<AuthoritativeEntityFactDto>(), responseLanguage),
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
                var depositPolicies = (await _unitOfWork.DepositPolicyRepository.GetAllOrderedAsync())
                    .Where(p => p.IsActive)
                    .ToList();
                var policySources = BuildPolicyGroundingSources(userPolicies, returnPolicies);

                var userPolicySection = BuildPolicySection(
                    isVietnamese ? "Chinh sach nguoi dung" : "User Policy",
                    userPolicies,
                    responseLanguage);
                var returnPolicySection = BuildPolicySection(
                    isVietnamese ? "Chinh sach hoan tra" : "Return Policy",
                    returnPolicies,
                    responseLanguage);
                var depositPolicySection = BuildDepositPolicySection(depositPolicies, responseLanguage);

                if (string.IsNullOrWhiteSpace(userPolicySection)
                    && string.IsNullOrWhiteSpace(returnPolicySection)
                    && string.IsNullOrWhiteSpace(depositPolicySection))
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

                if (!string.IsNullOrWhiteSpace(depositPolicySection))
                {
                    policySections.Add(depositPolicySection);
                }

                var groundedSummary = string.Join("\n", policySections);

                return new AIChatbotResponseDto
                {
                    Intent = ChatbotIntentPolicySupport,
                    Reply =
                        (isVietnamese
                            ? "Voi cau hoi ve chinh sach nguoi dung, hoan tra hoac dat coc, minh da lay thong tin dang active trong he thong:\n"
                            : "For user-policy, return-policy, or deposit-policy questions, I retrieved active policy information from the system:\n") +
                        groundedSummary + "\n" +
                        (isVietnamese
                            ? "De dam bao thong tin cuoi cung chinh xac tai thoi diem giao dich, ban vui long chat voi tu van vien. "
                            : "To ensure final accuracy at transaction time, please confirm with a support consultant. ") +
                        (isVietnamese
                            ? $"Ban cung co the xem chinh sach tai {_userPolicyDocumentPath} va {_returnPolicyDocumentPath}."
                            : $"You can also review policy documents at {_userPolicyDocumentPath} and {_returnPolicyDocumentPath}."),
                    SuggestedPlants = new List<PlantSuggestionResponseDto>(),
                    CareTips = new List<string>(),
                    FollowUpQuestions = BuildPolicyPromptSuggestions(responseLanguage),
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
                FollowUpQuestions = BuildPolicyPromptSuggestions(responseLanguage),
                PolicySources = new List<PolicyGroundingSourceDto>(),
                Disclaimer = isVietnamese
                    ? "Noi dung chinh sach co the thay doi theo thoi diem, vui long xac nhan voi tu van vien truoc khi thuc hien giao dich."
                    : "Policy content can change over time. Please confirm with a support consultant before any transaction.",
                UsedFallback = false
            };
        }

        // dữ liệu nội bộ cho AI để hiểu thực thể nào đang được đề xuất và trạng thái hiện tại của nó, tránh khuyến nghị sai lệch do embedding lỗi thời hoặc không chính xác.
        private async Task<List<AuthoritativeEntityFactDto>> BuildAuthoritativeFactsForSuggestionsAsync(
            List<PlantSuggestionResponseDto> suggestions,
            string responseLanguage)
        {
            if (suggestions.Count == 0)
            {
                return new List<AuthoritativeEntityFactDto>();
            }

            var facts = new List<AuthoritativeEntityFactDto>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var suggestion in suggestions)
            {
                var key = $"{suggestion.EntityType}:{suggestion.EntityId}";
                if (!seen.Add(key))
                {
                    continue;
                }

                var fact = suggestion.EntityType switch
                {
                    EmbeddingEntityTypes.CommonPlant => await BuildCommonPlantFactAsync(suggestion.EntityId, responseLanguage),
                    EmbeddingEntityTypes.PlantInstance => await BuildPlantInstanceFactAsync(suggestion.EntityId, responseLanguage),
                    EmbeddingEntityTypes.NurseryMaterial => await BuildNurseryMaterialFactAsync(suggestion.EntityId),
                    EmbeddingEntityTypes.NurseryPlantCombo => await BuildNurseryPlantComboFactAsync(suggestion.EntityId),
                    _ => null
                };

                if (fact != null)
                {
                    facts.Add(fact);
                }
            }

            return facts;
        }

        private async Task<AuthoritativeEntityFactDto?> BuildCommonPlantFactAsync(int entityId, string responseLanguage)
        {
            var commonPlant = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(entityId);
            if (commonPlant == null)
            {
                return null;
            }

            var plant = commonPlant.Plant;
            var guide = plant?.PlantGuide ?? (plant != null
                ? await _unitOfWork.PlantGuideRepository.GetByPlantIdWithPlantAsync(plant.Id)
                : null);
            var isPurchasable = commonPlant.IsActive && commonPlant.Quantity > 0;

            return new AuthoritativeEntityFactDto
            {
                EntityType = EmbeddingEntityTypes.CommonPlant,
                EntityId = commonPlant.Id,
                PlantId = commonPlant.PlantId,
                Name = plant?.Name ?? $"CommonPlant #{commonPlant.Id}",
                Description = plant?.Description,
                Price = plant?.BasePrice,
                NurseryId = commonPlant.NurseryId,
                NurseryName = commonPlant.Nursery?.Name,
                NurseryAddress = commonPlant.Nursery?.Address,
                IsActive = commonPlant.IsActive,
                IsPurchasable = isPurchasable,
                AvailabilityStatus = isPurchasable ? "Purchasable" : ResolveStockAvailability(commonPlant.IsActive, commonPlant.Quantity),
                Quantity = commonPlant.Quantity,
                Categories = GetCategoryNames(plant?.Categories),
                Tags = GetTagNames(plant?.Tags),
                RoomTypes = GetRoomTypeNames(plant?.RoomType),
                RoomStyles = GetRoomStyleNames(plant?.RoomStyle),
                FengShuiElement = MapFengShuiElement(plant?.FengShuiElement),
                FengShuiMeaning = plant?.FengShuiMeaning,
                PetSafe = plant?.PetSafe,
                ChildSafe = plant?.ChildSafe,
                AirPurifying = plant?.AirPurifying,
                LightRequirementName = GetLightRequirementName(guide?.LightRequirement),
                CareTips = guide == null
                    ? new List<string>()
                    : LocalizeCareTips(ExtractCareTipsFromGuide(guide, plant?.Name), responseLanguage)
            };
        }

        private async Task<AuthoritativeEntityFactDto?> BuildPlantInstanceFactAsync(int entityId, string responseLanguage)
        {
            var instance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(entityId);
            if (instance == null)
            {
                return null;
            }

            var plant = instance.Plant;
            var guide = plant?.PlantGuide ?? (plant != null
                ? await _unitOfWork.PlantGuideRepository.GetByPlantIdWithPlantAsync(plant.Id)
                : null);
            var statusName = Enum.IsDefined(typeof(PlantInstanceStatusEnum), instance.Status)
                ? ((PlantInstanceStatusEnum)instance.Status).ToString()
                : instance.Status.ToString();
            var isPurchasable = instance.Status == (int)PlantInstanceStatusEnum.Available;

            return new AuthoritativeEntityFactDto
            {
                EntityType = EmbeddingEntityTypes.PlantInstance,
                EntityId = instance.Id,
                PlantId = instance.PlantId,
                Name = plant?.Name ?? $"PlantInstance #{instance.Id}",
                Description = FirstNonEmpty(instance.Description, plant?.Description),
                Price = instance.SpecificPrice ?? plant?.BasePrice,
                NurseryId = instance.CurrentNurseryId,
                NurseryName = instance.CurrentNursery?.Name,
                NurseryAddress = instance.CurrentNursery?.Address,
                IsActive = isPurchasable,
                IsPurchasable = isPurchasable,
                AvailabilityStatus = isPurchasable ? "Purchasable" : statusName,
                StatusName = statusName,
                Categories = GetCategoryNames(plant?.Categories),
                Tags = GetTagNames(plant?.Tags),
                RoomTypes = GetRoomTypeNames(plant?.RoomType),
                RoomStyles = GetRoomStyleNames(plant?.RoomStyle),
                FengShuiElement = MapFengShuiElement(plant?.FengShuiElement),
                FengShuiMeaning = plant?.FengShuiMeaning,
                PetSafe = plant?.PetSafe,
                ChildSafe = plant?.ChildSafe,
                AirPurifying = plant?.AirPurifying,
                LightRequirementName = GetLightRequirementName(guide?.LightRequirement),
                CareTips = guide == null
                    ? new List<string>()
                    : LocalizeCareTips(ExtractCareTipsFromGuide(guide, plant?.Name), responseLanguage)
            };
        }

        private async Task<AuthoritativeEntityFactDto?> BuildNurseryMaterialFactAsync(int entityId)
        {
            var nurseryMaterial = await _unitOfWork.NurseryMaterialRepository.GetByIdWithDetailsAsync(entityId);
            if (nurseryMaterial == null)
            {
                return null;
            }

            var material = nurseryMaterial.Material;
            var isExpired = nurseryMaterial.ExpiredDate.HasValue
                && nurseryMaterial.ExpiredDate.Value <= DateOnly.FromDateTime(DateTime.Today);
            var isPurchasable = nurseryMaterial.IsActive && nurseryMaterial.Quantity > 0 && !isExpired;

            return new AuthoritativeEntityFactDto
            {
                EntityType = EmbeddingEntityTypes.NurseryMaterial,
                EntityId = nurseryMaterial.Id,
                MaterialId = nurseryMaterial.MaterialId,
                Name = material?.Name ?? $"NurseryMaterial #{nurseryMaterial.Id}",
                Description = material?.Description,
                Price = material?.BasePrice,
                NurseryId = nurseryMaterial.NurseryId,
                NurseryName = nurseryMaterial.Nursery?.Name,
                NurseryAddress = nurseryMaterial.Nursery?.Address,
                IsActive = nurseryMaterial.IsActive,
                IsPurchasable = isPurchasable,
                AvailabilityStatus = isExpired
                    ? "Expired"
                    : (isPurchasable ? "Purchasable" : ResolveStockAvailability(nurseryMaterial.IsActive, nurseryMaterial.Quantity)),
                Quantity = nurseryMaterial.Quantity,
                ExpiredDate = nurseryMaterial.ExpiredDate?.ToString("yyyy-MM-dd"),
                Categories = GetCategoryNames(material?.Categories),
                Tags = GetTagNames(material?.Tags),
                Brand = material?.Brand,
                Unit = material?.Unit,
                Specifications = material?.Specifications
            };
        }

        private async Task<AuthoritativeEntityFactDto?> BuildNurseryPlantComboFactAsync(int entityId)
        {
            var nurseryPlantCombo = await _unitOfWork.NurseryPlantComboRepository.GetQuery()
                .AsNoTracking()
                .Where(npc => npc.Id == entityId)
                .Include(npc => npc.Nursery)
                .Include(npc => npc.PlantCombo)
                    .ThenInclude(pc => pc.TagsNavigation)
                .FirstOrDefaultAsync();
            if (nurseryPlantCombo == null)
            {
                return null;
            }

            var combo = nurseryPlantCombo.PlantCombo;
            var comboActive = combo?.IsActive == true;
            var isPurchasable = nurseryPlantCombo.IsActive
                && nurseryPlantCombo.Quantity > 0
                && comboActive;

            return new AuthoritativeEntityFactDto
            {
                EntityType = EmbeddingEntityTypes.NurseryPlantCombo,
                EntityId = nurseryPlantCombo.Id,
                PlantComboId = nurseryPlantCombo.PlantComboId,
                Name = combo?.ComboName ?? $"NurseryPlantCombo #{nurseryPlantCombo.Id}",
                Description = combo?.Description,
                Price = combo?.ComboPrice,
                NurseryId = nurseryPlantCombo.NurseryId,
                NurseryName = nurseryPlantCombo.Nursery?.Name,
                NurseryAddress = nurseryPlantCombo.Nursery?.Address,
                IsActive = nurseryPlantCombo.IsActive && comboActive,
                IsPurchasable = isPurchasable,
                AvailabilityStatus = isPurchasable
                    ? "Purchasable"
                    : ResolveStockAvailability(nurseryPlantCombo.IsActive && comboActive, nurseryPlantCombo.Quantity),
                Quantity = nurseryPlantCombo.Quantity,
                Tags = GetTagNames(combo?.TagsNavigation),
                FengShuiElement = MapFengShuiElement(combo?.FengShuiElement),
                PetSafe = combo?.PetSafe,
                ChildSafe = combo?.ChildSafe,
                SuitableSpace = GetLightRequirementName(combo?.SuitableSpace),
                SuitableRooms = GetRoomTypeNames(combo?.SuitableRooms),
                Season = combo?.Season.HasValue == true && Enum.IsDefined(typeof(SeasonTypeEnum), combo.Season.Value)
                    ? ((SeasonTypeEnum)combo.Season.Value).ToString()
                    : null
            };
        }

        // dữ liệu để FE hiển thị
        private static void ApplyAuthoritativeFactsToSuggestions(
            List<PlantSuggestionResponseDto> suggestions,
            List<AuthoritativeEntityFactDto> facts)
        {
            foreach (var suggestion in suggestions)
            {
                var fact = facts.FirstOrDefault(f =>
                    f.EntityType == suggestion.EntityType
                    && f.EntityId == suggestion.EntityId);
                if (fact == null)
                {
                    suggestion.IsPurchasable = false;
                    continue;
                }

                suggestion.Name = fact.Name;
                suggestion.Description = fact.Description;
                suggestion.Price = fact.Price;
                suggestion.PlantId = fact.PlantId;
                suggestion.PlantComboId = fact.PlantComboId;
                suggestion.MaterialId = fact.MaterialId;
                suggestion.IsPurchasable = fact.IsPurchasable;
                suggestion.NurseryId = fact.NurseryId;
                suggestion.NurseryName = fact.NurseryName;
                suggestion.NurseryAddress = fact.NurseryAddress;
            }
        }

        private static string ResolveStockAvailability(bool isActive, int quantity)
        {
            if (!isActive)
            {
                return "Inactive";
            }

            return quantity > 0 ? "Purchasable" : "OutOfStock";
        }

        private static List<string> GetCategoryNames(IEnumerable<DataAccessLayer.Entities.Category>? categories)
        {
            return categories?
                .Select(c => c.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
        }

        private static List<string> GetTagNames(IEnumerable<DataAccessLayer.Entities.Tag>? tags)
        {
            return tags?
                .Select(t => t.TagName)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Select(n => n!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList() ?? new List<string>();
        }

        private static List<string> GetRoomTypeNames(List<int>? roomTypes)
        {
            return roomTypes?
                .Distinct()
                .Select(room => Enum.IsDefined(typeof(RoomTypeEnum), room)
                    ? ((RoomTypeEnum)room).ToString()
                    : room.ToString())
                .ToList() ?? new List<string>();
        }

        private static List<string> GetRoomStyleNames(List<int>? roomStyles)
        {
            return roomStyles?
                .Distinct()
                .Select(style => Enum.IsDefined(typeof(RoomStyleEnum), style)
                    ? ((RoomStyleEnum)style).ToString()
                    : style.ToString())
                .ToList() ?? new List<string>();
        }

        private static string? GetLightRequirementName(int? lightRequirement)
        {
            if (!lightRequirement.HasValue || !Enum.IsDefined(typeof(LightRequirementEnum), lightRequirement.Value))
            {
                return null;
            }

            return ((LightRequirementEnum)lightRequirement.Value).ToString();
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

        private static string BuildDepositPolicySection(
            List<DataAccessLayer.Entities.DepositPolicy>? policies,
            string responseLanguage)
        {
            if (policies == null || policies.Count == 0)
            {
                return string.Empty;
            }

            var isVietnamese = responseLanguage == LanguageVietnamese;
            var sectionTitle = isVietnamese ? "Chinh sach dat coc" : "Deposit Policy";
            var lines = policies
                .Where(p => p.IsActive)
                .OrderBy(p => p.MinPrice)
                .ThenBy(p => p.MaxPrice ?? decimal.MaxValue)
                .Select(p =>
                {
                    var priceRange = FormatDepositPriceRange(p.MinPrice, p.MaxPrice, isVietnamese);
                    return isVietnamese
                        ? $"- {priceRange}: dat coc {p.DepositPercentage}%"
                        : $"- {priceRange}: deposit {p.DepositPercentage}%";
                })
                .ToList();

            return lines.Count == 0
                ? string.Empty
                : sectionTitle + ":\n" + string.Join("\n", lines);
        }

        private static string FormatDepositPriceRange(decimal minPrice, decimal? maxPrice, bool isVietnamese)
        {
            var min = FormatPolicyMoney(minPrice);
            if (!maxPrice.HasValue)
            {
                return isVietnamese
                    ? $"tu {min} tro len"
                    : $"from {min} and above";
            }

            var max = FormatPolicyMoney(maxPrice.Value);
            return isVietnamese
                ? $"tu {min} den duoi {max}"
                : $"from {min} to under {max}";
        }

        private static string FormatPolicyMoney(decimal value)
        {
            return value.ToString("#,0.##", System.Globalization.CultureInfo.InvariantCulture);
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

        private static List<string> BuildUserPromptSuggestions(
            string intent,
            List<PlantSuggestionResponseDto>? suggestions,
            List<AuthoritativeEntityFactDto>? facts,
            string responseLanguage)
        {
            var isVietnamese = responseLanguage == LanguageVietnamese;
            var normalizedIntent = NormalizeIntent(intent);
            var suggestedItems = suggestions ?? new List<PlantSuggestionResponseDto>();
            var factItems = facts ?? new List<AuthoritativeEntityFactDto>();
            var primaryFact = factItems.FirstOrDefault(f => f.IsPurchasable) ?? factItems.FirstOrDefault();
            var primarySuggestion = suggestedItems.FirstOrDefault(s => s.IsPurchasable) ?? suggestedItems.FirstOrDefault();
            var plantName = FirstNonEmpty(primaryFact?.Name, primarySuggestion?.Name);
            var hasPurchasableItem = factItems.Any(f => f.IsPurchasable) || suggestedItems.Any(s => s.IsPurchasable);
            var prompts = new List<string>();

            if (normalizedIntent == ChatbotIntentPolicySupport)
            {
                return BuildPolicyPromptSuggestions(responseLanguage);
            }

            if (!string.IsNullOrWhiteSpace(plantName))
            {
                switch (normalizedIntent)
                {
                    case ChatbotIntentPlantCare:
                        AddPrompt(prompts, isVietnamese
                            ? $"Cho toi biet them cach cham soc {plantName}"
                            : $"Tell me more care tips for {plantName}");
                        AddPrompt(prompts, isVietnamese
                            ? $"Lich tuoi va anh sang phu hop cho {plantName} la gi?"
                            : $"What watering and light schedule is best for {plantName}?");
                        if (hasPurchasableItem)
                        {
                            AddPrompt(prompts, isVietnamese
                                ? $"Toi co the mua {plantName} o dau?"
                                : $"Where can I buy {plantName}?");
                        }
                        AddPrompt(prompts, isVietnamese
                            ? "Goi y cho toi cac cay tuong tu de cham soc"
                            : "Show me similar low-maintenance plants");
                        break;

                    case ChatbotIntentRoomEnvironment:
                        AddPrompt(prompts, isVietnamese
                            ? $"{plantName} co phu hop voi phong cua toi khong?"
                            : $"Is {plantName} suitable for my room?");
                        AddPrompt(prompts, isVietnamese
                            ? $"Toi nen dat {plantName} o vi tri nao trong phong?"
                            : $"Where should I place {plantName} in my room?");
                        if (hasPurchasableItem)
                        {
                            AddPrompt(prompts, isVietnamese
                                ? $"Toi co the mua {plantName} o dau?"
                                : $"Where can I buy {plantName}?");
                        }
                        AddPrompt(prompts, isVietnamese
                            ? "Goi y them cay phu hop voi phong cua toi"
                            : "Show me more plants suitable for my room");
                        break;

                    default:
                        AddPrompt(prompts, isVietnamese
                            ? $"Cho toi biet them ve {plantName}"
                            : $"Tell me more about {plantName}");
                        if (hasPurchasableItem)
                        {
                            AddPrompt(prompts, isVietnamese
                                ? $"Toi co the mua {plantName} o dau?"
                                : $"Where can I buy {plantName}?");
                        }
                        AddPrompt(prompts, isVietnamese
                            ? $"Cach cham soc {plantName} nhu the nao?"
                            : $"How do I care for {plantName}?");
                        AddPrompt(prompts, isVietnamese
                            ? "Goi y cho toi cac cay tuong tu"
                            : "Show me similar plants");
                        break;
                }
            }
            else
            {
                AddPrompt(prompts, isVietnamese
                    ? "Goi y cho toi cay canh de cham soc"
                    : "Suggest low-maintenance indoor plants");
                AddPrompt(prompts, isVietnamese
                    ? "Goi y cay phu hop voi phong cua toi"
                    : "Suggest plants suitable for my room");
                AddPrompt(prompts, isVietnamese
                    ? "Goi y cay theo ngan sach cua toi"
                    : "Suggest plants within my budget");
                AddPrompt(prompts, isVietnamese
                    ? "Huong dan toi cach cham soc cay"
                    : "Guide me on plant care");
            }

            return ClampTextList(prompts, MaxFollowUpsCount, MaxFollowUpChars);
        }

        private static List<string> BuildPolicyPromptSuggestions(string responseLanguage)
        {
            var isVietnamese = responseLanguage == LanguageVietnamese;
            return isVietnamese
                ? new List<string>
                {
                    "Tom tat ngan hon chinh sach nguoi dung",
                    "Tom tat ngan hon chinh sach hoan tra",
                    "Huong dan toi cach lien he tu van vien ho tro"
                }
                : new List<string>
                {
                    "Give me a shorter summary of the user policy",
                    "Give me a shorter summary of the return policy",
                    "Guide me on how to contact a support consultant"
                };
        }

        private static void AddPrompt(List<string> prompts, string? prompt)
        {
            if (!string.IsNullOrWhiteSpace(prompt))
            {
                prompts.Add(prompt.Trim());
            }
        }

        private static PlantSuggestionResponseDto MapRoomRecommendationToSuggestion(PlantRecommendationItemDto recommendation)
        {
            return new PlantSuggestionResponseDto
            {
                EntityType = recommendation.EntityType,
                EntityId = recommendation.EntityId,
                PlantId = recommendation.PlantId,
                PlantComboId = recommendation.PlantComboId,
                MaterialId = recommendation.MaterialId,
                Name = recommendation.Name,
                Description = recommendation.Description,
                Price = recommendation.Price,
                ImageUrl = recommendation.ImageUrl,
                IsPurchasable = true,
                RelevanceScore = recommendation.MatchScore,
                NurseryId = recommendation.NurseryId > 0 ? recommendation.NurseryId : null,
                NurseryName = recommendation.NurseryName,
                NurseryAddress = recommendation.NurseryAddress
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

        private static double ExtractCosineSimilarityScore(Dictionary<string, object>? metadata)
        {
            return TryReadMetadataDouble(metadata, "CosineSimilarityScore", out var score)
                ? score
                : 0;
        }

        private static bool TryReadMetadataDouble(Dictionary<string, object>? metadata, string key, out double value)
        {
            value = 0;
            if (metadata == null || !metadata.TryGetValue(key, out var rawValue) || rawValue == null)
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
                case string stringValue:
                    return double.TryParse(
                        stringValue,
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out value);
                case JsonElement jsonElement:
                    return TryReadJsonElementDouble(jsonElement, out value);
                default:
                    try
                    {
                        value = Convert.ToDouble(rawValue, System.Globalization.CultureInfo.InvariantCulture);
                        return true;
                    }
                    catch
                    {
                        return false;
                    }
            }
        }

        private static bool TryReadJsonElementDouble(JsonElement jsonElement, out double value)
        {
            value = 0;
            return jsonElement.ValueKind switch
            {
                JsonValueKind.Number => jsonElement.TryGetDouble(out value),
                JsonValueKind.String => double.TryParse(
                    jsonElement.GetString(),
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out value),
                _ => false
            };
        }

        private async Task<SearchResultItemDto?> EnrichSearchResultAsync(
            DataAccessLayer.Entities.Embedding embedding,
            int originalEntityId,
            bool isPurchasable)
        {
            var item = new SearchResultItemDto
            {
                EntityType = embedding.EntityType,
                EntityId = originalEntityId,
                IsPurchasable = isPurchasable,
                SimilarityScore = ExtractCosineSimilarityScore(embedding.Metadata)
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
                        item.PlantId = commonPlant.PlantId;
                        item.Name = commonPlant.Plant?.Name ?? "Unknown";
                        item.Description = commonPlant.Plant?.Description;
                        item.Price = commonPlant.Plant?.BasePrice;
                        item.NurseryId = commonPlant.NurseryId;
                        item.NurseryName = commonPlant.Nursery?.Name;
                        item.NurseryAddress = commonPlant.Nursery?.Address;
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
                        item.PlantId = instance.PlantId;
                        item.Name = instance.Plant?.Name ?? "Unknown";
                        item.Description = instance.Description ?? instance.Plant?.Description;
                        item.Price = instance.SpecificPrice ?? instance.Plant?.BasePrice;
                        item.NurseryId = instance.CurrentNurseryId ?? 0;
                        item.NurseryName = instance.CurrentNursery?.Name;
                        item.NurseryAddress = instance.CurrentNursery?.Address;
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
                        item.PlantComboId = combo.PlantComboId;
                        item.Name = combo.PlantCombo?.ComboName ?? "Unknown";
                        item.Description = combo.PlantCombo?.Description;
                        item.Price = combo.PlantCombo?.ComboPrice;
                        item.NurseryId = combo.NurseryId;
                        item.NurseryName = combo.Nursery?.Name;
                        item.NurseryAddress = combo.Nursery?.Address;
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
                        item.MaterialId = material.MaterialId;
                        item.Name = material.Material?.Name ?? "Unknown";
                        item.Description = material.Material?.Description;
                        item.Price = material.Material?.BasePrice;
                        item.NurseryId = material.NurseryId;
                        item.NurseryName = material.Nursery?.Name;
                        item.NurseryAddress = material.Nursery?.Address;
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

        private sealed class AuthoritativeEntityFactDto
        {
            public string EntityType { get; set; } = string.Empty;
            public int EntityId { get; set; }
            public int? PlantId { get; set; }
            public int? PlantComboId { get; set; }
            public int? MaterialId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public decimal? Price { get; set; }
            public int? NurseryId { get; set; }
            public string? NurseryName { get; set; }
            public string? NurseryAddress { get; set; }
            public bool? IsActive { get; set; }
            public bool IsPurchasable { get; set; }
            public string AvailabilityStatus { get; set; } = "Unknown";
            public int? Quantity { get; set; }
            public string? StatusName { get; set; }
            public string? ExpiredDate { get; set; }
            public List<string> Categories { get; set; } = new();
            public List<string> Tags { get; set; } = new();
            public List<string> RoomTypes { get; set; } = new();
            public List<string> RoomStyles { get; set; } = new();
            public string? FengShuiElement { get; set; }
            public string? FengShuiMeaning { get; set; }
            public bool? PetSafe { get; set; }
            public bool? ChildSafe { get; set; }
            public bool? AirPurifying { get; set; }
            public string? Brand { get; set; }
            public string? Unit { get; set; }
            public string? Specifications { get; set; }
            public string? SuitableSpace { get; set; }
            public List<string> SuitableRooms { get; set; } = new();
            public string? Season { get; set; }
            public string? LightRequirementName { get; set; }
            public List<string> CareTips { get; set; } = new();
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
