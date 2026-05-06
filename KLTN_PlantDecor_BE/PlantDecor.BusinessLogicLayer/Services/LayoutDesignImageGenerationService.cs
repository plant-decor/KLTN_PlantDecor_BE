using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class LayoutDesignImageGenerationService : ILayoutDesignImageGenerationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly HttpClient _httpClient;
        private readonly ILogger<LayoutDesignImageGenerationService> _logger;
        private readonly string _fluxEndpoint;
        private readonly string _fluxApiVersion;
        private readonly string _fluxApiKey;
        private readonly string _fluxModel;
        private readonly int _fluxWidth;
        private readonly int _fluxHeight;
        private readonly int _fluxN;

        public LayoutDesignImageGenerationService(
            IUnitOfWork unitOfWork,
            ICloudinaryService cloudinaryService,
            HttpClient httpClient,
            IConfiguration configuration,
            ILogger<LayoutDesignImageGenerationService> logger)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _httpClient = httpClient;
            _logger = logger;

            _fluxEndpoint = configuration["FluxImage:Endpoint"] ?? string.Empty;
            _fluxApiVersion = configuration["FluxImage:ApiVersion"] ?? "preview";
            _fluxApiKey = configuration["FluxImage:ApiKey"] ?? string.Empty;
            _fluxModel = configuration["FluxImage:Model"] ?? "FLUX.2-pro";
            _fluxWidth = configuration.GetValue("FluxImage:Width", 1024);
            _fluxHeight = configuration.GetValue("FluxImage:Height", 1024);
            _fluxN = configuration.GetValue("FluxImage:N", 1);
        }

        public async Task<LayoutDesignImageGenerationResultDto> GenerateImagesAsync(int layoutDesignId, int userId)
        {
            var itemResults = new List<LayoutDesignImageGenerationItemResultDto>();
            var transactionStarted = false;
            var transactionCommitted = false;

            try
            {
                // nếu endpoint hoặc api key chưa cấu hình thì không nên tiếp tục, tránh mất công tải ảnh về rồi mới báo lỗi
                EnsureFluxConfigured();

                var layout = await _unitOfWork.LayoutDesignRepository.GetGenerationContextByIdAsync(layoutDesignId);
                if (layout == null)
                {
                    throw new NotFoundException($"LayoutDesign {layoutDesignId} was not found");
                }

                // kiểm tra quyền sở hữu có đúng của user không
                EnsureLayoutOwnership(layout, userId);
                // chỉ cho phép khi status là ImageGenerationCompleted hoặc PlantRecommendationCompleted
                EnsureLayoutStatusAllowed(layout.Status);

                var roomImageUrl = ResolveRoomImageUrl(layout);
                if (string.IsNullOrWhiteSpace(roomImageUrl))
                {
                    throw new BadRequestException("Room image URL is missing for the selected layout");
                }

                var layoutPlants = layout.LayoutDesignPlants
                    .OrderBy(item => item.Id)
                    .ToList();

                if (layoutPlants.Count == 0)
                {
                    throw new BadRequestException("LayoutDesign does not contain any recommended plants");
                }

                // down ảnh phòng và mã hóa base64
                var roomImageBase64 = await DownloadAsBase64Async(roomImageUrl, layoutDesignId, 0);

                // lấy ra danh sách commonPlant từ LayoutDesignPlant
                var commonPlantIds = layoutPlants
                    .Where(item => item.CommonPlantId.HasValue)
                    .Select(item => item.CommonPlantId!.Value)
                    .Distinct()
                    .ToList();

                // lấy ra danh sách PlantInstance từ LayoutDesignPlant
                var plantInstanceIds = layoutPlants
                    .Where(item => item.PlantInstanceId.HasValue)
                    .Select(item => item.PlantInstanceId!.Value)
                    .Distinct()
                    .ToList();

                // Lấy ra URL ảnh chính của CommonPlant và PlantInstance để dùng làm ảnh nguồn cho việc tạo ảnh mới với Flux
                var commonPlantImageUrls = await _unitOfWork.CommonPlantRepository.GetPrimaryImageUrlsAsync(commonPlantIds);
                var plantInstanceImageUrls = await _unitOfWork.PlantInstanceRepository.GetPrimaryImageUrlsAsync(plantInstanceIds);

                var candidates = BuildCandidates(layoutPlants, commonPlantImageUrls, plantInstanceImageUrls, itemResults);
                var roomDesignContext = await BuildRoomDesignContextAsync(layout);

                await _unitOfWork.BeginTransactionAsync();
                transactionStarted = true;

                try
                {
                    if (candidates.Count == 0)
                    {
                        QueueAiLayoutItemModerations(layoutDesignId, itemResults);
                        await _unitOfWork.CommitTransactionAsync();
                        transactionCommitted = true;
                        throw new BadRequestException("No valid plant rows found for image generation");
                    }

                    var existingImages = await _unitOfWork.LayoutDesignAiResponseImageRepository.GetByLayoutDesignIdAsync(layoutDesignId);
                    foreach (var existingImage in existingImages)
                    {
                        var deleted = false;

                        if (!string.IsNullOrWhiteSpace(existingImage.PublicId))
                        {
                            deleted = await _cloudinaryService.DeleteFileAsync(existingImage.PublicId);
                        }
                        else if (!string.IsNullOrWhiteSpace(existingImage.ImageUrl))
                        {
                            deleted = await _cloudinaryService.DeleteFileByUrlAsync(existingImage.ImageUrl);
                        }

                        if (!deleted && (!string.IsNullOrWhiteSpace(existingImage.PublicId) || !string.IsNullOrWhiteSpace(existingImage.ImageUrl)))
                        {
                            _logger.LogWarning(
                                "Failed to delete old Cloudinary image for layout {LayoutDesignId}: PublicId={PublicId}, ImageUrl={ImageUrl}",
                                layoutDesignId,
                                existingImage.PublicId,
                                existingImage.ImageUrl);

                            if (!string.IsNullOrWhiteSpace(existingImage.PublicId) && !string.IsNullOrWhiteSpace(existingImage.ImageUrl))
                            {
                                deleted = await _cloudinaryService.DeleteFileByUrlAsync(existingImage.ImageUrl);
                            }
                        }

                        _unitOfWork.LayoutDesignAiResponseImageRepository.PrepareRemove(existingImage);
                    }

                    var orderedCandidates = candidates
                        .OrderBy(candidate => candidate.LayoutDesignPlantId)
                        .ToList();

                    try
                    {
                        foreach (var candidate in orderedCandidates)
                        {
                            await ProcessCandidateAsync(layout, candidate, roomImageBase64, roomDesignContext, itemResults);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to generate images for layout {LayoutDesignId}", layout.Id);
                        throw;
                    }

                    var trackedLayout = await _unitOfWork.LayoutDesignRepository.GetByIdAsync(layoutDesignId);
                    if (trackedLayout != null)
                    {
                        var allSucceeded = itemResults.Count > 0 && itemResults.All(item => item.IsSuccess);
                        trackedLayout.Status = allSucceeded
                            ? (int)LayoutDesignStatusEnum.ImageGenerationCompleted
                            : (int)LayoutDesignStatusEnum.PlantRecommendationCompleted;

                        _unitOfWork.LayoutDesignRepository.PrepareUpdate(trackedLayout);
                    }

                    QueueAiLayoutItemModerations(layoutDesignId, itemResults);
                    await _unitOfWork.CommitTransactionAsync();
                    transactionCommitted = true;

                    var successCount = itemResults.Count(item => item.IsSuccess);
                    return new LayoutDesignImageGenerationResultDto
                    {
                        LayoutDesignId = layoutDesignId,
                        TotalItems = itemResults.Count,
                        SuccessCount = successCount,
                        FailureCount = itemResults.Count - successCount,
                        StatusAfter = itemResults.All(item => item.IsSuccess)
                            ? (int)LayoutDesignStatusEnum.ImageGenerationCompleted
                            : (int)LayoutDesignStatusEnum.PlantRecommendationCompleted,
                        Items = itemResults
                    };
                }
                catch
                {
                    if (transactionStarted && !transactionCommitted)
                    {
                        await _unitOfWork.RollbackTransactionAsync();
                    }

                    throw;
                }
            }
            catch (Exception ex)
            {
                // fallback moderation chỉ thực hiện cho lỗi sớm trước transaction để tránh lưu nhầm pending changes sau rollback
                if (!transactionStarted)
                {
                    await SaveAiLayoutModerationAsync(layoutDesignId, AilayoutResponseModerationStatus.Rejected, ex.Message);
                }

                throw;
            }
        }

        public async Task<List<LayoutDesignGeneratedImageDto>> GetGeneratedImagesAsync(int layoutDesignId, int userId)
        {
            var layout = await _unitOfWork.LayoutDesignRepository.GetByIdAsync(layoutDesignId);
            if (layout == null)
            {
                throw new NotFoundException($"LayoutDesign {layoutDesignId} was not found");
            }

            EnsureLayoutOwnership(layout, userId);

            var images = await _unitOfWork.LayoutDesignAiResponseImageRepository.GetByLayoutDesignIdAsync(layoutDesignId);
            return images.ToLayoutDesignGeneratedImageDtoList();
        }

        public async Task<List<LayoutDesignGeneratedImageDto>> GetAllGeneratedImagesByUserIdAsync(int userId)
        {
            var images = await _unitOfWork.LayoutDesignAiResponseImageRepository.GetAllGeneratedImagesByUserIdAsync(userId);
            return images.ToLayoutDesignGeneratedImageDtoList();
        }

        private async Task ProcessCandidateAsync(
            LayoutDesign layout,
            GenerationCandidate candidate,
            string roomImageBase64,
            string roomDesignContext,
            List<LayoutDesignImageGenerationItemResultDto> itemResults)
        {
            var result = new LayoutDesignImageGenerationItemResultDto
            {
                LayoutDesignPlantId = candidate.LayoutDesignPlantId,
                CommonPlantId = candidate.CommonPlantId,
                PlantInstanceId = candidate.PlantInstanceId,
                PlacementPosition = candidate.PlacementPosition
            };

            try
            {
                var plantImageBase64 = await DownloadAsBase64Async(
                    candidate.PlantImageUrl,
                    layout.Id,
                    candidate.LayoutDesignPlantId);

                var prompt = BuildFluxPrompt(layout.Id, candidate, roomDesignContext);
                var generatedBytes = await GenerateImageWithFluxAsync(prompt, plantImageBase64, roomImageBase64);

                var fileName = $"layout_{layout.Id}_plant_{candidate.LayoutDesignPlantId}_{DateTime.UtcNow:yyyyMMddHHmmssfff}.png";
                var uploadResult = await _cloudinaryService.UploadImageBytesAsync(generatedBytes, fileName, "LayoutDesignAI");

                var entity = new LayoutDesignAiResponseImage
                {
                    LayoutDesignId = layout.Id,
                    LayoutDesignPlantId = candidate.LayoutDesignPlantId,
                    ImageUrl = uploadResult.SecureUrl,
                    PublicId = uploadResult.PublicId,
                    FluxPromptUsed = prompt,
                    CreatedAt = DateTime.UtcNow
                };

                _unitOfWork.LayoutDesignAiResponseImageRepository.PrepareCreate(entity);

                result.IsSuccess = true;
                result.ImageUrl = uploadResult.SecureUrl;
                result.FluxPromptUsed = prompt;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to generate image for layout {LayoutDesignId} - layoutPlant {LayoutDesignPlantId}",
                    layout.Id,
                    candidate.LayoutDesignPlantId);

                result.IsSuccess = false;
                result.ErrorCode = "GENERATION_FAILED";
                result.ErrorMessage = ex.Message;
            }

            itemResults.Add(result);
        }

        private static List<GenerationCandidate> BuildCandidates(
            IReadOnlyCollection<LayoutDesignPlant> layoutPlants,
            IReadOnlyDictionary<int, string> commonPlantImageUrls,
            IReadOnlyDictionary<int, string> plantInstanceImageUrls,
            ICollection<LayoutDesignImageGenerationItemResultDto> itemResults)
        {
            var candidates = new List<GenerationCandidate>();

            foreach (var item in layoutPlants)
            {
                // nếu cả commonPlant và PlantInstance đều null thì trả về lỗi
                if (!item.CommonPlantId.HasValue && !item.PlantInstanceId.HasValue)
                {
                    itemResults.Add(new LayoutDesignImageGenerationItemResultDto
                    {
                        LayoutDesignPlantId = item.Id,
                        PlacementPosition = item.PlacementPosition,
                        IsSuccess = false,
                        ErrorCode = "INVALID_SOURCE",
                        ErrorMessage = "Both CommonPlantId and PlantInstanceId are null"
                    });
                    continue;
                }

                // nếu CommonPlantId có giá trị và Url không null/empty thì thêm vào candidate
                if (item.CommonPlantId.HasValue
                    && commonPlantImageUrls.TryGetValue(item.CommonPlantId.Value, out var commonPlantImageUrl)
                    && !string.IsNullOrWhiteSpace(commonPlantImageUrl))
                {
                    candidates.Add(new GenerationCandidate
                    {
                        LayoutDesignPlantId = item.Id,
                        CommonPlantId = item.CommonPlantId,
                        PlantInstanceId = item.PlantInstanceId,
                        PlacementPosition = item.PlacementPosition,
                        PlantImageUrl = commonPlantImageUrl,
                        SourceType = "CommonPlant",
                        SourceEntityId = item.CommonPlantId.Value
                    });
                    continue;
                }

                // nếu PlantInstanceId có giá trị và Url không null/empty thì thêm vào candidate
                if (item.PlantInstanceId.HasValue
                    && plantInstanceImageUrls.TryGetValue(item.PlantInstanceId.Value, out var plantInstanceImageUrl)
                    && !string.IsNullOrWhiteSpace(plantInstanceImageUrl))
                {
                    candidates.Add(new GenerationCandidate
                    {
                        LayoutDesignPlantId = item.Id,
                        CommonPlantId = item.CommonPlantId,
                        PlantInstanceId = item.PlantInstanceId,
                        PlacementPosition = item.PlacementPosition,
                        PlantImageUrl = plantInstanceImageUrl,
                        SourceType = "PlantInstance",
                        SourceEntityId = item.PlantInstanceId.Value
                    });
                    continue;
                }

                itemResults.Add(new LayoutDesignImageGenerationItemResultDto
                {
                    LayoutDesignPlantId = item.Id,
                    CommonPlantId = item.CommonPlantId,
                    PlantInstanceId = item.PlantInstanceId,
                    PlacementPosition = item.PlacementPosition,
                    IsSuccess = false,
                    ErrorCode = "SOURCE_IMAGE_NOT_FOUND",
                    ErrorMessage = "Unable to resolve source image URL from CommonPlant/PlantInstance"
                });
            }

            return candidates;
        }

        private async Task<string> DownloadAsBase64Async(string imageUrl, int layoutDesignId, int layoutDesignPlantId)
        {
            try
            {
                var response = await _httpClient.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();
                var bytes = await response.Content.ReadAsByteArrayAsync();

                if (bytes.Length == 0)
                {
                    throw new Exception("Downloaded image is empty");
                }

                return Convert.ToBase64String(bytes);
            }
            catch (Exception ex)
            {
                throw new Exception(
                    $"Failed to download image for layout {layoutDesignId}, layoutPlant {layoutDesignPlantId}",
                    ex);
            }
        }

        private async Task<byte[]> GenerateImageWithFluxSingleShotAsync(
            string prompt,
            string roomImageBase64,
            IReadOnlyCollection<string> plantImageBase64List)
        {
            if (plantImageBase64List == null || plantImageBase64List.Count == 0)
            {
                throw new InvalidOperationException("At least one plant reference image is required");
            }

            var requestBody = new Dictionary<string, object>
            {
                ["prompt"] = prompt,
                ["width"] = _fluxWidth,
                ["height"] = _fluxHeight,
                ["n"] = _fluxN,
                ["model"] = _fluxModel,
                ["input_image"] = roomImageBase64
            };

            var inputIndex = 2;
            foreach (var plantImageBase64 in plantImageBase64List)
            {
                requestBody[$"input_image_{inputIndex}"] = plantImageBase64;
                inputIndex++;
            }

            var request = new HttpRequestMessage(HttpMethod.Post, BuildFluxUrl())
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _fluxApiKey);

            var response = await _httpClient.SendAsync(request);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException(
                    $"Flux API request failed with {(int)response.StatusCode}: {Truncate(responseText, 300)}");
            }

            var base64Image = ExtractBase64Image(responseText);
            if (string.IsNullOrWhiteSpace(base64Image))
            {
                throw new InvalidOperationException("Flux API response does not contain data[0].b64_json");
            }

            try
            {
                return Convert.FromBase64String(base64Image);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Flux API returned invalid base64 image", ex);
            }
        }

        // Backward-compatible wrappers kept for legacy helper usage.
        private static string BuildFluxPrompt(int layoutDesignId, GenerationCandidate candidate, string roomDesignContext)
        {
            _ = layoutDesignId;
            return BuildFluxSinglePlantPrompt(candidate, roomDesignContext);
        }

        private Task<byte[]> GenerateImageWithFluxAsync(string prompt, string plantImageBase64, string roomImageBase64)
        {
            return GenerateImageWithFluxSingleShotAsync(prompt, roomImageBase64, new[] { plantImageBase64 });
        }

        private static string? ExtractBase64Image(string responseText)
        {
            using var document = JsonDocument.Parse(responseText);
            var root = document.RootElement;

            if (TryExtractBase64FromRoot(root, out var fromRoot))
            {
                return fromRoot;
            }

            if (root.TryGetProperty("result", out var resultElement)
                && TryExtractBase64FromRoot(resultElement, out var fromResult))
            {
                return fromResult;
            }

            return null;
        }

        private static bool TryExtractBase64FromRoot(JsonElement root, out string? base64)
        {
            base64 = null;
            if (!root.TryGetProperty("data", out var dataElement)
                || dataElement.ValueKind != JsonValueKind.Array
                || dataElement.GetArrayLength() == 0)
            {
                return false;
            }

            var firstItem = dataElement[0];
            if (!firstItem.TryGetProperty("b64_json", out var b64Element)
                || b64Element.ValueKind != JsonValueKind.String)
            {
                return false;
            }

            base64 = b64Element.GetString();
            return !string.IsNullOrWhiteSpace(base64);
        }

        private void QueueAiLayoutItemModerations(int layoutDesignId, IEnumerable<LayoutDesignImageGenerationItemResultDto> itemResults)
        {
            foreach (var item in itemResults)
            {
                var status = item.IsSuccess
                    ? AilayoutResponseModerationStatus.Approved
                    : AilayoutResponseModerationStatus.Rejected;

                var moderation = new AilayoutResponseModeration
                {
                    LayoutDesignId = layoutDesignId,
                    Status = (int)status,
                    Reason = TrimAndLimit(BuildAiLayoutModerationReason(item), 255),
                    ReviewedAt = DateTime.UtcNow
                };

                _unitOfWork.AiLayoutResponseModerationRepository.PrepareCreate(moderation);
            }
        }

        private async Task SaveAiLayoutModerationAsync(
            int? layoutDesignId,
            AilayoutResponseModerationStatus status,
            string? reason)
        {
            try
            {
                var defaultReason = status == AilayoutResponseModerationStatus.Approved
                    ? "AI layout image generated successfully"
                    : "AI layout image generation failed";

                var moderation = new AilayoutResponseModeration
                {
                    LayoutDesignId = layoutDesignId,
                    Status = (int)status,
                    Reason = TrimAndLimit(string.IsNullOrWhiteSpace(reason) ? defaultReason : reason, 255),
                    ReviewedAt = DateTime.UtcNow
                };

                _unitOfWork.AiLayoutResponseModerationRepository.PrepareCreate(moderation);
                await _unitOfWork.SaveAsync();
            }
            catch (Exception saveEx)
            {
                _logger.LogError(saveEx, "Failed to save AI layout moderation. LayoutDesignId={LayoutDesignId}", layoutDesignId);
            }
        }

        private static string BuildAiLayoutModerationReason(LayoutDesignImageGenerationItemResultDto item)
        {
            if (item.IsSuccess)
            {
                return $"LayoutDesignPlantId={item.LayoutDesignPlantId}: image generated successfully";
            }

            var errorCode = string.IsNullOrWhiteSpace(item.ErrorCode) ? "GENERATION_FAILED" : item.ErrorCode;
            var errorMessage = string.IsNullOrWhiteSpace(item.ErrorMessage) ? "Image generation failed" : item.ErrorMessage;

            return $"LayoutDesignPlantId={item.LayoutDesignPlantId}: {errorCode} - {errorMessage}";
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

        private static string BuildFluxMultiPlantPrompt(IReadOnlyCollection<GenerationCandidate> candidates)
        {
            var plantLines = candidates
                .OrderBy(candidate => candidate.LayoutDesignPlantId)
                .Select((candidate, index) =>
                {
                    var inputImageIndex = index + 2;
                    var placement = string.IsNullOrWhiteSpace(candidate.PlacementPosition)
                        ? "a visually appropriate position in the room"
                        : candidate.PlacementPosition;

                    return $@"Insert Plant {(char)('A' + index)} using reference image input_image_{inputImageIndex}, which corresponds to {candidate.SourceType} #{candidate.SourceEntityId}. Keep Plant {(char)('A' + index)} visually consistent with its reference identity, including species silhouette, leaf/flower structure, dominant color appearance, and overall morphology. Place Plant {(char)('A' + index)} at {placement}. Plant {(char)('A' + index)} must appear exactly once and must not be duplicated, cloned, mirrored, or repeated as a similar variant.";
                });

            return $@"You are given one room image (image 1) and multiple plant reference images. Compose one final image by inserting all required plants into image 1. Do not add any extra plants, decorative flowers, or additional potted vegetation. The final image must contain exactly {plantLines.Count()} inserted plants in total, no more and no less.

Execute these placement instructions exactly:

{string.Join("\n\n", plantLines)}

Hard constraints: keep all required plants in the same final image and never duplicate any plant. Never substitute a reference plant with another species or a visually similar alternative. Never split one reference into multiple plants. Never add background plants on balconies, shelves, corners, or outside the requested set. Preserve realistic lighting, shadows, perspective, and scale for every inserted plant. Do not modify existing furniture, walls, architecture, or room layout. Avoid unnatural overlap between plants and ensure each inserted plant is grounded on a real support surface such as floor, table, or furniture. Respect depth and camera perspective at all times. The output is valid only if every inserted plant matches its reference identity and the total inserted plant count is exactly {plantLines.Count()}.";
        }

        private static string BuildFluxSinglePlantPrompt(GenerationCandidate candidate, string roomDesignContext)
        {
            var placement = string.IsNullOrWhiteSpace(candidate.PlacementPosition)
                ? "a visually appropriate position in the room"
                : candidate.PlacementPosition;

            return $@"You are given one room image (image 1) and one plant reference image (image 2).

Task:
Insert exactly one new plant from the reference into the room image, while allowing non-structural restyling of the room.

Room design context:
{roomDesignContext}

What you may change (non-structural):
- Repaint walls and ceilings, update materials and surface finishes, and adjust colors.
- Replace or reposition movable furniture.
- Add decor items and artwork that fit the requested style (plant-themed art is allowed).
- Adjust textiles, lighting fixtures, and accessories.

Hard structural constraints:
- Keep the room layout and geometry unchanged.
- Do not move, resize, remove, or redraw doors or windows.
- Do not warp or bend walls, floor, or ceiling planes.

Plant instructions:
- Reference image: input_image_2
- Description: plant from {candidate.SourceType} #{candidate.SourceEntityId}
- Placement: {placement}

Plant requirements:
- Add exactly one new plant in the final image.
- If the original room already contains plants, keep them unchanged; do not remove or duplicate them.
- Do not add any other new plants, potted plants, flower arrangements, or greenery beyond the single required plant.
- Do not introduce extra planters or plant stands unless they belong to the required plant.
- The inserted plant must not be duplicated, mirrored, or fused with existing plants or objects.
- Match the plant identity, silhouette, and dominant color appearance to the reference.
- Maintain realistic lighting, shadows, scale, and perspective.
- The plant must sit on a real support surface and the base must touch the surface.
- Avoid unnatural overlap with room objects.

Important:
- This is a SINGLE plant composition task (exactly one new plant).
- The room may be restyled, but the architecture and openings must stay fixed";
        }

        private async Task<string> BuildRoomDesignContextAsync(LayoutDesign layout)
        {
            var primaryRoomImageId = ResolvePrimaryRoomImageId(layout);
            if (!primaryRoomImageId.HasValue)
            {
                return "No room design preferences were found. Keep the existing room structure unchanged while allowing decorative restyling.";
            }

            var preferences = (await _unitOfWork.RoomDesignPreferencesRepository.GetAllAsync())
                .FirstOrDefault(item => item.RoomImageId == primaryRoomImageId.Value);

            if (preferences == null)
            {
                return "No room design preferences were found. Keep the existing room structure unchanged while allowing decorative restyling.";
            }

            var roomType = MapNullableEnum(preferences.RoomType, typeof(RoomTypeEnum), "unspecified");
            var roomStyle = MapNullableEnum(preferences.RoomStyle, typeof(RoomStyleEnum), "unspecified");
            var lightDirection = MapNullableEnum(preferences.LightDirection, typeof(DirectionEnum), "unspecified");
            var dominantDirection = MapNullableEnum(preferences.DominantDirection, typeof(DirectionEnum), "unspecified");
            var naturalLight = MapNullableEnum(preferences.NaturalLightLevel, typeof(LightRequirementEnum), "unspecified");
            var roomArea = preferences.RoomArea.HasValue
                ? preferences.RoomArea.Value.ToString("0.##")
                : "unspecified";

            return $@"Requested room type: {roomType}
Requested room style: {roomStyle}
Room area: {roomArea} m2
Light direction: {lightDirection}
Dominant direction: {dominantDirection}
Natural light level: {naturalLight}

Allowed changes:
- Restyle the room freely to match the requested style.
- Add decorative objects, textiles, accents, and furniture that fit the request.
- Adjust colors, materials, and surface finishes to improve the design.

Hard constraints:
- Do not alter the room structure.
- Keep doors and windows exactly as they are in the input image.
- Do not move, resize, remove, or redraw any door or window.
- Preserve wall boundaries, openings, and architectural proportions.";
        }

        private static int? ResolvePrimaryRoomImageId(LayoutDesign layout)
        {
            return layout.LayoutDesignRoomImages
                .OrderBy(link => link.ViewAngle == (int)RoomViewAngleEnum.Front ? 0 : 1)
                .ThenBy(link => link.OrderIndex ?? int.MaxValue)
                .ThenBy(link => link.RoomImageId)
                .Select(link => (int?)link.RoomImageId)
                .FirstOrDefault();
        }

        private static string MapNullableEnum(int? value, Type enumType, string fallback)
        {
            if (!value.HasValue || !Enum.IsDefined(enumType, value.Value))
            {
                return fallback;
            }

            return Enum.GetName(enumType, value.Value) ?? fallback;
        }

        private string BuildFluxUrl()
        {
            if (_fluxEndpoint.Contains("api-version=", StringComparison.OrdinalIgnoreCase))
            {
                return _fluxEndpoint;
            }

            var separator = _fluxEndpoint.Contains('?') ? "&" : "?";
            return $"{_fluxEndpoint}{separator}api-version={_fluxApiVersion}";
        }

        private static string? ResolveRoomImageUrl(LayoutDesign layout)
        {
            var roomImageUrl = layout.LayoutDesignRoomImages
                .OrderBy(link => link.ViewAngle == (int)RoomViewAngleEnum.Front ? 0 : 1)
                .ThenBy(link => link.OrderIndex ?? int.MaxValue)
                .ThenBy(link => link.RoomImageId)
                .Select(link => link.RoomImage.ImageUrl)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));

            if (!string.IsNullOrWhiteSpace(roomImageUrl))
            {
                return roomImageUrl;
            }

            if (!string.IsNullOrWhiteSpace(layout.PreviewImageUrl))
            {
                return layout.PreviewImageUrl;
            }

            return null;
        }

        private static void EnsureLayoutOwnership(LayoutDesign layout, int userId)
        {
            if (!layout.UserId.HasValue)
            {
                throw new ForbiddenException("LayoutDesign is not owned by an authenticated user");
            }

            if (layout.UserId.Value != userId)
            {
                throw new ForbiddenException("You do not have permission to access this layout");
            }
        }

        private static void EnsureLayoutStatusAllowed(int? status)
        {
            if (status == (int)LayoutDesignStatusEnum.PlantRecommendationCompleted
                || status == (int)LayoutDesignStatusEnum.ImageGenerationCompleted)
            {
                return;
            }

            throw new BadRequestException("LayoutDesign is not ready for image generation");
        }

        private void EnsureFluxConfigured()
        {
            if (string.IsNullOrWhiteSpace(_fluxEndpoint))
            {
                throw new InvalidOperationException("FluxImage:Endpoint is not configured");
            }

            if (string.IsNullOrWhiteSpace(_fluxApiKey))
            {
                throw new InvalidOperationException("FluxImage:ApiKey is not configured");
            }
        }

        private static string Truncate(string? text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            return text.Length <= maxLength
                ? text
                : text[..maxLength];
        }

        private sealed class GenerationCandidate
        {
            public int LayoutDesignPlantId { get; set; }
            public int? CommonPlantId { get; set; }
            public int? PlantInstanceId { get; set; }
            public string? PlacementPosition { get; set; }
            public string PlantImageUrl { get; set; } = string.Empty;
            public string SourceType { get; set; } = string.Empty;
            public int SourceEntityId { get; set; }
        }
    }
}
