using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class LayoutDesignManualEditorService : ILayoutDesignManualEditorService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<LayoutDesignManualEditorService> _logger;
        private readonly string _fluxEndpoint;
        private readonly string _fluxApiVersion;
        private readonly string _fluxApiKey;
        private readonly string _fluxModel;
        private readonly int _fluxWidth;
        private readonly int _fluxHeight;
        private readonly int _fluxN;

        public LayoutDesignManualEditorService(
            IUnitOfWork unitOfWork,
            ICloudinaryService cloudinaryService,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<LayoutDesignManualEditorService> logger)
        {
            _unitOfWork = unitOfWork;
            _cloudinaryService = cloudinaryService;
            _httpClientFactory = httpClientFactory;
            _logger = logger;

            _fluxEndpoint = configuration["FluxImage:Endpoint"] ?? string.Empty;
            _fluxApiVersion = configuration["FluxImage:ApiVersion"] ?? "preview";
            _fluxApiKey = configuration["FluxImage:ApiKey"] ?? string.Empty;
            _fluxModel = configuration["FluxImage:Model"] ?? "FLUX.2-pro";
            _fluxWidth = configuration.GetValue("FluxImage:Width", 1024);
            _fluxHeight = configuration.GetValue("FluxImage:Height", 1024);
            _fluxN = configuration.GetValue("FluxImage:N", 1);
        }

        public async Task<LayoutDesignManualEditorContextDto> GetEditorContextAsync(int layoutDesignId, int userId)
        {
            var layout = await _unitOfWork.LayoutDesignRepository.GetGenerationContextByIdAsync(layoutDesignId);
            if (layout == null)
            {
                throw new NotFoundException($"LayoutDesign {layoutDesignId} was not found");
            }

            EnsureLayoutOwnership(layout, userId);

            var roomImageUrl = ResolveRoomImageUrl(layout);
            if (string.IsNullOrWhiteSpace(roomImageUrl))
            {
                throw new BadRequestException("Room image URL is missing for this layout");
            }

            var images = await _unitOfWork.LayoutDesignAiResponseImageRepository.GetByLayoutDesignIdAsync(layoutDesignId);
            var filteredImages = FilterImagesForEditor(images);

            var useFullCatalog = images.Count == 0 || layout.LayoutDesignPlants.Count == 0;
            var plants = useFullCatalog
                ? await BuildFullCatalogPlantsAsync()
                : await BuildRecommendedPlantsAsync(layout.LayoutDesignPlants);

            return new LayoutDesignManualEditorContextDto
            {
                LayoutDesignId = layoutDesignId,
                RoomImageUrl = roomImageUrl,
                IsFullCatalog = useFullCatalog,
                Plants = plants,
                Images = filteredImages.Select(MapEditorImage).ToList()
            };
        }

        public async Task<LayoutDesignManualEditorImageDto> SaveCompositeDraftAsync(int layoutDesignId, int userId, string layerJson)
        {
            if (string.IsNullOrWhiteSpace(layerJson))
            {
                throw new BadRequestException("LayerJson is required");
            }

            var layout = await EnsureLayoutAsync(layoutDesignId, userId);

            var draft = await GetOrCreateManualDraftAsync(layout.Id, null);
            draft.ManualLayerJson = layerJson.Trim();
            draft.ManualEditedBy = userId;
            draft.ManualEditedAt = DateTime.UtcNow;

            await SaveManualDraftAsync(draft);

            return MapEditorImage(draft);
        }

        public async Task<LayoutDesignManualEditorImageDto> SavePlantDraftAsync(int layoutDesignId, int layoutDesignPlantId, int userId, string layerJson)
        {
            if (string.IsNullOrWhiteSpace(layerJson))
            {
                throw new BadRequestException("LayerJson is required");
            }

            var layout = await EnsureLayoutAsync(layoutDesignId, userId);
            var layoutPlant = await EnsureLayoutPlantAsync(layout.Id, layoutDesignPlantId);

            var draft = await GetOrCreateManualDraftAsync(layout.Id, layoutPlant.Id);
            draft.ManualLayerJson = layerJson.Trim();
            draft.ManualEditedBy = userId;
            draft.ManualEditedAt = DateTime.UtcNow;

            await SaveManualDraftAsync(draft);

            return MapEditorImage(draft);
        }

        public async Task<LayoutDesignManualEditorImageDto> PublishCompositeAsync(int layoutDesignId, int userId, IFormFile imageFile, string? layerJson)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                throw new BadRequestException("Manual image file is required");
            }

            var layout = await EnsureLayoutAsync(layoutDesignId, userId);

            var uploadResult = await _cloudinaryService.UploadFileAsync(imageFile, "LayoutDesignManual");

            var manualImage = new LayoutDesignAiResponseImage
            {
                LayoutDesignId = layout.Id,
                LayoutDesignPlantId = null,
                ImageUrl = uploadResult.SecureUrl,
                PublicId = uploadResult.PublicId,
                SourceType = (int)LayoutDesignImageSourceTypeEnum.Manual,
                ManualLayerJson = TrimAndLimit(layerJson, 4000),
                ManualEditedBy = userId,
                ManualEditedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.LayoutDesignAiResponseImageRepository.PrepareCreate(manualImage);
            await _unitOfWork.SaveAsync();

            return MapEditorImage(manualImage);
        }

        public async Task<LayoutDesignManualEditorImageDto> PublishPlantAsync(int layoutDesignId, int layoutDesignPlantId, int userId, IFormFile imageFile, string? layerJson)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                throw new BadRequestException("Manual image file is required");
            }

            var layout = await EnsureLayoutAsync(layoutDesignId, userId);
            var layoutPlant = await EnsureLayoutPlantAsync(layout.Id, layoutDesignPlantId);

            var uploadResult = await _cloudinaryService.UploadFileAsync(imageFile, "LayoutDesignManual");
            var replacesImageId = await ResolveAiImageIdToReplaceAsync(layout.Id, layoutPlant.Id);

            var manualImage = new LayoutDesignAiResponseImage
            {
                LayoutDesignId = layout.Id,
                LayoutDesignPlantId = layoutPlant.Id,
                ImageUrl = uploadResult.SecureUrl,
                PublicId = uploadResult.PublicId,
                SourceType = (int)LayoutDesignImageSourceTypeEnum.Manual,
                ManualLayerJson = TrimAndLimit(layerJson, 4000),
                ManualEditedBy = userId,
                ManualEditedAt = DateTime.UtcNow,
                ReplacesImageId = replacesImageId,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.LayoutDesignAiResponseImageRepository.PrepareCreate(manualImage);
            await _unitOfWork.SaveAsync();

            return MapEditorImage(manualImage);
        }

        public async Task<LayoutDesignManualEditorImageDto> BeautifyCompositeAsync(
            int layoutDesignId,
            int userId,
            IFormFile? imageFile,
            string? imageUrl,
            string? layerJson)
        {
            if ((imageFile == null || imageFile.Length == 0) && string.IsNullOrWhiteSpace(imageUrl))
            {
                throw new BadRequestException("Image file or ImageUrl is required");
            }

            EnsureFluxConfigured();

            var layout = await EnsureLayoutAsync(layoutDesignId, userId);
            var inputBytes = await ResolveInputImageBytesAsync(imageFile, imageUrl, layoutDesignId);
            var inputBase64 = Convert.ToBase64String(inputBytes);
            var prompt =
    "Enhance the realism of the existing plants already placed in the room.\r\n\r\nDo NOT add, remove, reposition, resize, or replace any plants or furniture.\r\nKeep the exact original room layout, camera angle, composition, and object placement.\r\n\r\nMake all existing plants blend naturally into the environment by matching:\r\n- indoor lighting\r\n- shadow direction and softness\r\n- floor contact shadows\r\n- reflections and occlusion\r\n- color temperature\r\n- contrast and saturation\r\n- camera blur/noise level\r\n- perspective and depth\r\n\r\nThe plants should not look overly sharp, overly saturated, or visually detached from the room.\r\nThey should feel subtly integrated into the scene as if they were naturally photographed there.\r\n\r\nDo NOT generate additional plants or decorations.\r\nPhotorealistic indoor smartphone photo. For example, focus on the plant in the theme like the following image: https://res.cloudinary.com/dliirxsmo/image/upload/v1779127811/LayoutDesignBeautify/LayoutDesignBeautify/a655a07d-5852-455a-9724-c5f59e401ed4_layout_7_beautify_20260518181008187.jpg.";

            var generatedBytes = await GenerateBeautifyImageAsync(prompt, inputBase64);
            var fileName = $"layout_{layout.Id}_beautify_{DateTime.UtcNow:yyyyMMddHHmmssfff}.png";
            var uploadResult = await _cloudinaryService.UploadImageBytesAsync(generatedBytes, fileName, "LayoutDesignBeautify");

            var manualImage = new LayoutDesignAiResponseImage
            {
                LayoutDesignId = layout.Id,
                LayoutDesignPlantId = null,
                ImageUrl = uploadResult.SecureUrl,
                PublicId = uploadResult.PublicId,
                FluxPromptUsed = prompt,
                SourceType = (int)LayoutDesignImageSourceTypeEnum.Manual,
                ManualLayerJson = TrimAndLimit(layerJson, 4000),
                ManualEditedBy = userId,
                ManualEditedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.LayoutDesignAiResponseImageRepository.PrepareCreate(manualImage);
            await _unitOfWork.SaveAsync();

            return MapEditorImage(manualImage);
        }

        public async Task<LayoutDesignManualCalculateResponseDto> CalculateManualTotalAsync(
            int layoutDesignId,
            int userId,
            LayoutDesignManualCalculateRequestDto request)
        {
            if (request == null || request.Items == null || request.Items.Count == 0)
            {
                throw new BadRequestException("Items are required");
            }

            _ = await EnsureLayoutAsync(layoutDesignId, userId);

            var errors = new List<LayoutDesignManualCalculateErrorDto>();
            var commonQuantities = new Dictionary<int, int>();
            var instanceQuantities = new Dictionary<int, int>();

            for (var i = 0; i < request.Items.Count; i++)
            {
                var item = request.Items[i];
                var hasCommon = item.CommonPlantId.HasValue;
                var hasInstance = item.PlantInstanceId.HasValue;

                if (hasCommon == hasInstance)
                {
                    errors.Add(new LayoutDesignManualCalculateErrorDto
                    {
                        Index = i,
                        CommonPlantId = item.CommonPlantId,
                        PlantInstanceId = item.PlantInstanceId,
                        ErrorCode = "INVALID_IDENTIFIER",
                        ErrorMessage = "Exactly one of CommonPlantId or PlantInstanceId is required"
                    });
                    continue;
                }

                if (item.Quantity <= 0)
                {
                    errors.Add(new LayoutDesignManualCalculateErrorDto
                    {
                        Index = i,
                        CommonPlantId = item.CommonPlantId,
                        PlantInstanceId = item.PlantInstanceId,
                        ErrorCode = "INVALID_QUANTITY",
                        ErrorMessage = "Quantity must be greater than 0"
                    });
                    continue;
                }

                if (hasCommon)
                {
                    var id = item.CommonPlantId!.Value;
                    commonQuantities[id] = commonQuantities.TryGetValue(id, out var qty)
                        ? qty + item.Quantity
                        : item.Quantity;
                }
                else
                {
                    var id = item.PlantInstanceId!.Value;
                    instanceQuantities[id] = instanceQuantities.TryGetValue(id, out var qty)
                        ? qty + item.Quantity
                        : item.Quantity;
                }
            }

            var lineItems = new List<LayoutDesignManualCalculateLineItemDto>();

            if (commonQuantities.Count > 0)
            {
                var commonPlants = await _unitOfWork.CommonPlantRepository.GetByIdsAsync(commonQuantities.Keys.ToList());
                var foundIds = commonPlants.Select(cp => cp.Id).ToHashSet();

                foreach (var missingId in commonQuantities.Keys.Except(foundIds))
                {
                    errors.Add(new LayoutDesignManualCalculateErrorDto
                    {
                        CommonPlantId = missingId,
                        ErrorCode = "NOT_FOUND",
                        ErrorMessage = $"CommonPlant {missingId} was not found"
                    });
                }

                foreach (var commonPlant in commonPlants)
                {
                    var quantity = commonQuantities[commonPlant.Id];
                    var unitPrice = commonPlant.Plant?.BasePrice ?? 0;
                    lineItems.Add(new LayoutDesignManualCalculateLineItemDto
                    {
                        CommonPlantId = commonPlant.Id,
                        Name = commonPlant.Plant?.Name ?? string.Empty,
                        UnitPrice = unitPrice,
                        Quantity = quantity,
                        SubTotal = unitPrice * quantity
                    });
                }
            }

            if (instanceQuantities.Count > 0)
            {
                var instances = await _unitOfWork.PlantInstanceRepository.GetByIdsAsync(instanceQuantities.Keys.ToList());
                var foundIds = instances.Select(instance => instance.Id).ToHashSet();

                foreach (var missingId in instanceQuantities.Keys.Except(foundIds))
                {
                    errors.Add(new LayoutDesignManualCalculateErrorDto
                    {
                        PlantInstanceId = missingId,
                        ErrorCode = "NOT_FOUND",
                        ErrorMessage = $"PlantInstance {missingId} was not found"
                    });
                }

                foreach (var instance in instances)
                {
                    var quantity = instanceQuantities[instance.Id];
                    var unitPrice = instance.SpecificPrice ?? instance.Plant?.BasePrice ?? 0;
                    lineItems.Add(new LayoutDesignManualCalculateLineItemDto
                    {
                        PlantInstanceId = instance.Id,
                        Name = instance.Plant?.Name ?? string.Empty,
                        UnitPrice = unitPrice,
                        Quantity = quantity,
                        SubTotal = unitPrice * quantity
                    });
                }
            }

            var orderedItems = lineItems
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.CommonPlantId ?? item.PlantInstanceId ?? 0)
                .ToList();

            var total = orderedItems.Sum(item => item.SubTotal);

            return new LayoutDesignManualCalculateResponseDto
            {
                Items = orderedItems,
                Errors = errors,
                TotalAmount = total
            };
        }

        private async Task<LayoutDesign> EnsureLayoutAsync(int layoutDesignId, int userId)
        {
            var layout = await _unitOfWork.LayoutDesignRepository.GetByIdAsync(layoutDesignId);
            if (layout == null)
            {
                throw new NotFoundException($"LayoutDesign {layoutDesignId} was not found");
            }

            EnsureLayoutOwnership(layout, userId);
            return layout;
        }

        private async Task<LayoutDesignPlant> EnsureLayoutPlantAsync(int layoutDesignId, int layoutDesignPlantId)
        {
            var layoutPlant = await _unitOfWork.LayoutDesignPlantRepository.GetByIdAsync(layoutDesignPlantId);
            if (layoutPlant == null || layoutPlant.LayoutDesignId != layoutDesignId)
            {
                throw new NotFoundException("LayoutDesignPlant was not found for this layout");
            }

            return layoutPlant;
        }

        private async Task<LayoutDesignAiResponseImage> GetOrCreateManualDraftAsync(int layoutDesignId, int? layoutDesignPlantId)
        {
            var images = await _unitOfWork.LayoutDesignAiResponseImageRepository.GetByLayoutDesignIdAsync(layoutDesignId);
            var existing = images
                .Where(image => IsManualImage(image) && image.LayoutDesignPlantId == layoutDesignPlantId)
                .OrderByDescending(image => image.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(image => image.Id)
                .FirstOrDefault();

            if (existing != null)
            {
                return existing;
            }

            var draft = new LayoutDesignAiResponseImage
            {
                LayoutDesignId = layoutDesignId,
                LayoutDesignPlantId = layoutDesignPlantId,
                SourceType = (int)LayoutDesignImageSourceTypeEnum.Manual,
                CreatedAt = DateTime.UtcNow
            };

            _unitOfWork.LayoutDesignAiResponseImageRepository.PrepareCreate(draft);
            await _unitOfWork.SaveAsync();
            return draft;
        }

        private async Task SaveManualDraftAsync(LayoutDesignAiResponseImage draft)
        {
            _unitOfWork.LayoutDesignAiResponseImageRepository.PrepareUpdate(draft);
            await _unitOfWork.SaveAsync();
        }

        private async Task<int?> ResolveAiImageIdToReplaceAsync(int layoutDesignId, int layoutDesignPlantId)
        {
            var images = await _unitOfWork.LayoutDesignAiResponseImageRepository.GetByLayoutDesignIdAsync(layoutDesignId);
            var aiImage = images
                .Where(image => image.LayoutDesignPlantId == layoutDesignPlantId && !IsManualImage(image))
                .OrderByDescending(image => image.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(image => image.Id)
                .FirstOrDefault();

            return aiImage?.Id;
        }

        private async Task<List<LayoutDesignManualEditorPlantDto>> BuildRecommendedPlantsAsync(IEnumerable<LayoutDesignPlant> layoutPlants)
        {
            var plants = layoutPlants.ToList();
            if (plants.Count == 0)
            {
                return new List<LayoutDesignManualEditorPlantDto>();
            }

            var commonPlantIds = plants
                .Where(item => item.CommonPlantId.HasValue)
                .Select(item => item.CommonPlantId!.Value)
                .Distinct()
                .ToList();

            var plantInstanceIds = plants
                .Where(item => item.PlantInstanceId.HasValue)
                .Select(item => item.PlantInstanceId!.Value)
                .Distinct()
                .ToList();

            var commonPlantImageUrls = await _unitOfWork.CommonPlantRepository.GetPrimaryImageUrlsAsync(commonPlantIds);
            var plantInstanceImageUrls = await _unitOfWork.PlantInstanceRepository.GetPrimaryImageUrlsAsync(plantInstanceIds);

            return plants.Select(item =>
            {
                var name = item.PlantInstance?.Plant?.Name
                           ?? item.CommonPlant?.Plant?.Name
                           ?? string.Empty;

                var imageUrl = item.CommonPlantId.HasValue
                    && commonPlantImageUrls.TryGetValue(item.CommonPlantId.Value, out var commonUrl)
                    ? commonUrl
                    : null;

                if (imageUrl == null && item.PlantInstanceId.HasValue
                    && plantInstanceImageUrls.TryGetValue(item.PlantInstanceId.Value, out var instanceUrl))
                {
                    imageUrl = instanceUrl;
                }

                return new LayoutDesignManualEditorPlantDto
                {
                    LayoutDesignPlantId = item.Id,
                    CommonPlantId = item.CommonPlantId,
                    PlantInstanceId = item.PlantInstanceId,
                    Name = name,
                    ImageUrl = imageUrl,
                    PlacementPosition = item.PlacementPosition,
                    IsRecommended = true
                };
            }).ToList();
        }

        private async Task<List<LayoutDesignManualEditorPlantDto>> BuildFullCatalogPlantsAsync()
        {
            var commonPlants = await _unitOfWork.CommonPlantRepository.GetAllActiveWithDetailsAsync();
            var plantInstances = await _unitOfWork.PlantInstanceRepository.GetAllAvailableWithDetailsAsync();

            var items = new List<LayoutDesignManualEditorPlantDto>();

            foreach (var commonPlant in commonPlants)
            {
                var imageUrl = SelectPrimaryImageUrl(commonPlant.Plant?.PlantImages);
                items.Add(new LayoutDesignManualEditorPlantDto
                {
                    CommonPlantId = commonPlant.Id,
                    Name = commonPlant.Plant?.Name ?? string.Empty,
                    ImageUrl = imageUrl,
                    IsRecommended = false
                });
            }

            foreach (var instance in plantInstances)
            {
                var imageUrl = SelectPrimaryImageUrl(instance.PlantImages)
                    ?? SelectPrimaryImageUrl(instance.Plant?.PlantImages);

                items.Add(new LayoutDesignManualEditorPlantDto
                {
                    PlantInstanceId = instance.Id,
                    Name = instance.Plant?.Name ?? string.Empty,
                    ImageUrl = imageUrl,
                    IsRecommended = false
                });
            }

            return items
                .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private LayoutDesignManualEditorImageDto MapEditorImage(LayoutDesignAiResponseImage image)
        {
            return new LayoutDesignManualEditorImageDto
            {
                ImageId = image.Id,
                LayoutDesignId = image.LayoutDesignId,
                LayoutDesignPlantId = image.LayoutDesignPlantId,
                ImageUrl = image.ImageUrl,
                SourceType = image.SourceType.HasValue
                    ? (LayoutDesignImageSourceTypeEnum)image.SourceType.Value
                    : null,
                ReplacesImageId = image.ReplacesImageId,
                ManualLayerJson = image.ManualLayerJson,
                CreatedAt = image.CreatedAt
            };
        }

        private static string? ResolveRoomImageUrl(LayoutDesign layout)
        {
            var roomImageUrl = layout.LayoutDesignRoomImages
                .OrderBy(link => link.OrderIndex == 1 ? 0 : 1)
                .ThenBy(link => link.OrderIndex ?? int.MaxValue)
                .ThenBy(link => link.RoomImageId)
                .Select(link => link.RoomImage.ImageUrl)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));

            if (!string.IsNullOrWhiteSpace(roomImageUrl))
            {
                return roomImageUrl;
            }

            return layout.PreviewImageUrl;
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

        private static bool IsManualImage(LayoutDesignAiResponseImage image)
        {
            return image.SourceType.HasValue &&
                   image.SourceType.Value == (int)LayoutDesignImageSourceTypeEnum.Manual;
        }

        private static List<LayoutDesignAiResponseImage> FilterImagesForEditor(IEnumerable<LayoutDesignAiResponseImage> images)
        {
            var materialized = images?.ToList() ?? new List<LayoutDesignAiResponseImage>();
            if (materialized.Count == 0)
            {
                return materialized;
            }

            var manualByPlant = materialized
                .Where(image => IsManualImage(image) && image.LayoutDesignPlantId.HasValue)
                .GroupBy(image => image.LayoutDesignPlantId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(image => image.CreatedAt ?? DateTime.MinValue)
                        .ThenByDescending(image => image.Id)
                        .First());

            var aiByPlant = materialized
                .Where(image => !IsManualImage(image) && image.LayoutDesignPlantId.HasValue)
                .GroupBy(image => image.LayoutDesignPlantId!.Value)
                .ToDictionary(
                    group => group.Key,
                    group => group.OrderByDescending(image => image.CreatedAt ?? DateTime.MinValue)
                        .ThenByDescending(image => image.Id)
                        .First());

            var selected = new List<LayoutDesignAiResponseImage>();
            foreach (var plantId in manualByPlant.Keys.Union(aiByPlant.Keys))
            {
                if (manualByPlant.TryGetValue(plantId, out var manual))
                {
                    selected.Add(manual);
                    continue;
                }

                if (aiByPlant.TryGetValue(plantId, out var ai))
                {
                    selected.Add(ai);
                }
            }

            var manualComposite = materialized
                .Where(image => IsManualImage(image) && !image.LayoutDesignPlantId.HasValue)
                .OrderByDescending(image => image.CreatedAt ?? DateTime.MinValue)
                .ThenByDescending(image => image.Id)
                .FirstOrDefault();

            if (manualComposite != null)
            {
                selected.Add(manualComposite);
            }

            return selected;
        }

        private static string? SelectPrimaryImageUrl(IEnumerable<PlantImage>? images)
        {
            if (images == null)
            {
                return null;
            }

            return images
                .Where(image => !string.IsNullOrWhiteSpace(image.ImageUrl))
                .OrderByDescending(image => image.IsPrimary == true)
                .Select(image => image.ImageUrl)
                .FirstOrDefault();
        }

        private async Task<byte[]> ResolveInputImageBytesAsync(IFormFile? imageFile, string? imageUrl, int layoutDesignId)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                using var memoryStream = new MemoryStream();
                await imageFile.CopyToAsync(memoryStream);
                return memoryStream.ToArray();
            }

            if (string.IsNullOrWhiteSpace(imageUrl))
            {
                throw new BadRequestException("Image file or ImageUrl is required");
            }

            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var response = await httpClient.GetAsync(imageUrl);
                response.EnsureSuccessStatusCode();
                var bytes = await response.Content.ReadAsByteArrayAsync();

                if (bytes.Length == 0)
                {
                    throw new InvalidOperationException("Downloaded image is empty");
                }

                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download image for layout {LayoutDesignId}", layoutDesignId);
                throw new BadRequestException("Unable to download image from ImageUrl");
            }
        }

        private async Task<byte[]> GenerateBeautifyImageAsync(string prompt, string inputImageBase64)
        {
            var requestBody = new Dictionary<string, object>
            {
                ["prompt"] = prompt,
                ["width"] = _fluxWidth,
                ["height"] = _fluxHeight,
                ["n"] = _fluxN,
                ["model"] = _fluxModel,
                ["input_image"] = inputImageBase64
            };

            var request = new HttpRequestMessage(HttpMethod.Post, BuildFluxUrl())
            {
                Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _fluxApiKey);

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.SendAsync(request);
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

        private string BuildFluxUrl()
        {
            if (_fluxEndpoint.Contains("api-version=", StringComparison.OrdinalIgnoreCase))
            {
                return _fluxEndpoint;
            }

            var separator = _fluxEndpoint.Contains('?') ? "&" : "?";
            return $"{_fluxEndpoint}{separator}api-version={_fluxApiVersion}";
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
    }
}
