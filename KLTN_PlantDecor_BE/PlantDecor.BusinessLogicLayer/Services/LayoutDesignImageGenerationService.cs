using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

            var itemResults = new List<LayoutDesignImageGenerationItemResultDto>();
            var candidates = BuildCandidates(layoutPlants, commonPlantImageUrls, plantInstanceImageUrls, itemResults);

            if (candidates.Count == 0)
            {
                throw new BadRequestException("No valid plant rows found for image generation");
            }

            await _unitOfWork.BeginTransactionAsync();
            try
            {
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

                foreach (var candidate in candidates)
                {
                    await ProcessCandidateAsync(layout, candidate, roomImageBase64, itemResults);
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

                await _unitOfWork.CommitTransactionAsync();

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
                await _unitOfWork.RollbackTransactionAsync();
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
            return images.Select(image => new LayoutDesignGeneratedImageDto
            {
                Id = image.Id,
                ImageUrl = image.ImageUrl,
                FluxPromptUsed = image.FluxPromptUsed,
                CreatedAt = image.CreatedAt
            }).ToList();
        }

        private async Task ProcessCandidateAsync(
            LayoutDesign layout,
            GenerationCandidate candidate,
            string roomImageBase64,
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

                var prompt = BuildFluxPrompt(layout.Id, candidate);
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

        private async Task<byte[]> GenerateImageWithFluxAsync(string prompt, string plantImageBase64, string roomImageBase64)
        {
            var requestBody = new
            {
                prompt,
                width = _fluxWidth,
                height = _fluxHeight,
                n = _fluxN,
                model = _fluxModel,
                input_image = plantImageBase64,
                input_image_2 = roomImageBase64
            };

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

        private static string BuildFluxPrompt(int layoutDesignId, GenerationCandidate candidate)
        {
            var placement = string.IsNullOrWhiteSpace(candidate.PlacementPosition)
                ? "a visually appropriate position in the room"
                : candidate.PlacementPosition;

            return $"Add the potted plant from the second reference image into this room. " +
            $"Place it exactly {placement}. " +
            $"Maintain the original lighting, shadows, and all existing furniture or wall art. " +
            $"Most Important: Do not modify any other part of the room.";
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
            if (!string.IsNullOrWhiteSpace(layout.RoomImage?.ImageUrl))
            {
                return layout.RoomImage.ImageUrl;
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
