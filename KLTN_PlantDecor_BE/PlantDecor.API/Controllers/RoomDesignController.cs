using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Helpers;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// AI-powered room design and plant recommendation endpoints
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class RoomDesignController : ControllerBase
    {
        private readonly IRoomDesignService _roomDesignService;
        private readonly ILayoutDesignImageGenerationService _layoutDesignImageGenerationService;
        private readonly ILogger<RoomDesignController> _logger;

        public RoomDesignController(
            IRoomDesignService roomDesignService,
            ILayoutDesignImageGenerationService layoutDesignImageGenerationService,
            ILogger<RoomDesignController> logger)
        {
            _roomDesignService = roomDesignService;
            _layoutDesignImageGenerationService = layoutDesignImageGenerationService;
            _logger = logger;
        }

        /// <summary>
        /// Get active Plant options for allergy selection.
        /// </summary>
        [HttpGet("allergy-plants")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllergyPlantOptions([FromQuery] string? keyword = null, [FromQuery] int take = 50)
        {
            var options = await _roomDesignService.GetAllergyPlantOptionsAsync(keyword, take);
            return Ok(new
            {
                success = true,
                data = options,
                message = $"Found {options.Count} active plants"
            });
        }

        /// <summary>
        /// Get all layout designs of the authenticated user.
        /// Each layout includes related layout plants and generated AI response images.
        /// </summary>
        [HttpGet("layouts")]
        [Authorize]
        [ProducesResponseType(typeof(PaginatedResult<LayoutDesignListResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllLayouts([FromQuery] Pagination pagination)
        {
            try
            {
                var userId = GetRequiredUserId();
                var layouts = await _roomDesignService.GetAllLayoutsAsync(userId, pagination);

                return Ok(new ApiResponse<PaginatedResult<LayoutDesignListResponseDto>>
                {
                    Success = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Get all layouts successfully",
                    Payload = layouts
                });
            }
            catch (UnauthorizedException ex)
            {
                throw new UnauthorizedException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while getting layouts");
                throw new Exception("Failed to get layouts. Please try again.");
            }
        }

        // Analyze room image and get plant recommendations.
        // Kept as commented reference for the legacy JSON base64 endpoint.
        //[HttpPost("analyze")]
        //[ProducesResponseType(typeof(RoomDesignResponseDto), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> AnalyzeAndRecommend([FromBody] RoomDesignRequestDto request)
        //{
        //    if (string.IsNullOrWhiteSpace(request.RoomImageBase64))
        //    {
        //        return BadRequest(new { success = false, message = "Room image is required" });
        //    }

        //    // Validate base64 image
        //    var normalizedImageBase64 = NormalizeBase64(request.RoomImageBase64);
        //    try
        //    {
        //        var imageBytes = Convert.FromBase64String(normalizedImageBase64);
        //        if (imageBytes.Length > 10 * 1024 * 1024) // 10MB limit
        //        {
        //            return BadRequest(new { success = false, message = "Image size exceeds 10MB limit" });
        //        }
        //    }
        //    catch (FormatException)
        //    {
        //        return BadRequest(new { success = false, message = "Invalid base64 image format" });
        //    }

        //    try
        //    {
        //        request.RoomImageBase64 = normalizedImageBase64;
        //        var result = await _roomDesignService.AnalyzeAndRecommendAsync(request);

        //        return Ok(new
        //        {
        //            success = true,
        //            data = result,
        //            message = $"Found {result.TotalCount} plant recommendations"
        //        });
        //    }
        //    catch (BadRequestException ex)
        //    {
        //        _logger.LogWarning(ex, "Invalid room design request");
        //        return BadRequest(new
        //        {
        //            success = false,
        //            message = ex.Message
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error in room design analysis");
        //        return StatusCode(500, new
        //        {
        //            success = false,
        //            message = "Failed to analyze room. Please try again."
        //        });
        //    }
        //}

        /// <summary>
        /// Analyze room image and get plant recommendations from multipart form-data upload.
        /// </summary>
        /// <remarks>
        /// This endpoint uses AI to:
        /// 1. Analyze the room image (type, size, lighting, style)
        /// 2. Search for suitable plants in the database
        /// 3. Return personalized recommendations with explanations
        ///
        /// All recommended plants are guaranteed to be:
        /// - Available in the database
        /// - Currently purchasable (in stock)
        /// - Matching the specified filters (budget, feng shui, etc.)
        /// </remarks>
        /// <param name="request">Multipart request containing image and optional filters</param>
        /// <returns>Room analysis and plant recommendations</returns>
        /// <response code="200">Successfully analyzed room and generated recommendations</response>
        /// <response code="400">Invalid request (missing image, etc.)</response>
        /// <response code="401">Unauthorized request (user not logged in)</response>
        /// <response code="500">AI processing error</response>
        [HttpPost("analyze-upload")]
        [Authorize(Roles = "Customer")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(RoomDesignResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AnalyzeAndRecommendUpload([FromForm] AnalyzeAndRecommendUploadRequest request)
        {
            try
            {
                if (request.Image == null || request.Image.Length == 0)
                {
                    throw new BadRequestException("Room image file is required");
                }

                var userId = GetRequiredUserId();
                var result = await _roomDesignService.AnalyzeAndRecommendUploadAsync(request, userId);

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = $"Found {result.TotalCount} plant recommendations"
                });
            }
            catch (UnauthorizedException ex)
            {
                throw new UnauthorizedException(ex.Message);
            }
            catch (BadRequestException ex)
            {
                _logger.LogWarning(ex, "Invalid room design upload request");
                throw new BadRequestException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in room design analysis (multipart)");
                throw new Exception("Failed to analyze room. Please try again.");
            }
        }

        // Analyze room image only (without plant recommendations).
        // Kept as commented reference for the legacy JSON base64 endpoint.
        //[HttpPost("analyze-room-only")]
        //[ProducesResponseType(typeof(RoomAnalysisDto), StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //public async Task<IActionResult> AnalyzeRoomOnly([FromBody] AnalyzeRoomOnlyRequest request)
        //{
        //    if (string.IsNullOrWhiteSpace(request.ImageBase64))
        //    {
        //        return BadRequest(new { success = false, message = "Room image is required" });
        //    }

        //    var normalizedImageBase64 = NormalizeBase64(request.ImageBase64);
        //    try
        //    {
        //        var imageBytes = Convert.FromBase64String(normalizedImageBase64);
        //        if (imageBytes.Length > 10 * 1024 * 1024) // 10MB limit
        //        {
        //            return BadRequest(new { success = false, message = "Image size exceeds 10MB limit" });
        //        }
        //    }
        //    catch (FormatException)
        //    {
        //        return BadRequest(new { success = false, message = "Invalid base64 image format" });
        //    }

        //    try
        //    {
        //        var result = await _roomDesignService.AnalyzeRoomAsync(normalizedImageBase64);

        //        return Ok(new
        //        {
        //            success = true,
        //            data = result
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error analyzing room");
        //        return StatusCode(500, new
        //        {
        //            success = false,
        //            message = "Failed to analyze room"
        //        });
        //    }
        //}

        /// <summary>
        /// Analyze room image only from multipart form-data upload.
        /// </summary>
        /// <remarks>
        /// Quick analysis of room characteristics without searching for plants.
        /// Useful for previewing the analysis before getting recommendations.
        /// </remarks>
        /// <param name="request">Multipart request containing image file</param>
        /// <returns>Room analysis result</returns>
        [HttpPost("analyze-room-only-upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(RoomAnalysisDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AnalyzeRoomOnlyUpload([FromForm] AnalyzeRoomOnlyUploadRequest request)
        {
            if (request.Image == null || request.Image.Length == 0)
            {
                throw new BadRequestException("Room image file is required");
            }

            if (request.Image.Length > 10 * 1024 * 1024) // 10MB limit
            {
                throw new BadRequestException("Image size exceeds 10MB limit");
            }

            try
            {
                await using var stream = request.Image.OpenReadStream();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var imageBase64 = Convert.ToBase64String(memoryStream.ToArray());

                var result = await _roomDesignService.AnalyzeRoomAsync(imageBase64);
                return Ok(new ApiResponse<RoomAnalysisDto>
                {
                    Success = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = "Room analysis successful",
                    Payload = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing room (multipart)");
                throw new Exception("Failed to analyze room. Please try again.");
            }
        }

        /// <summary>
        /// Generate AI room images from existing LayoutDesign recommendations.
        /// </summary>
        [HttpPost("{layoutDesignId:int}/generate-images")]
        [Authorize]
        [ProducesResponseType(typeof(LayoutDesignImageGenerationResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GenerateImages(int layoutDesignId)
        {
            try
            {
                var userId = GetRequiredUserId();
                var result = await _layoutDesignImageGenerationService.GenerateImagesAsync(layoutDesignId, userId);

                if (result.SuccessCount == 0)
                {
                    throw new Exception("Image generation failed for all items");
                }

                return Ok(new ApiResponse<LayoutDesignImageGenerationResultDto>
                {
                    Success = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = $"Generated {result.SuccessCount}/{result.TotalItems} images",
                    Payload = result
                });
            }
            catch (UnauthorizedException ex)
            {
                throw new UnauthorizedException(ex.Message);
            }
            catch (ForbiddenException ex)
            {
                throw new ForbiddenException(ex.Message);
            }
            catch (NotFoundException ex)
            {
                throw new NotFoundException(ex.Message);
            }
            catch (BadRequestException ex)
            {
                throw new BadRequestException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while generating images for LayoutDesign {LayoutDesignId}", layoutDesignId);
                throw new Exception("Failed to generate images. Please try again.");
            }
        }

        /// <summary>
        /// Get generated AI images for a LayoutDesign.
        /// </summary>
        [HttpGet("{layoutDesignId:int}/generated-images")]
        [Authorize]
        [ProducesResponseType(typeof(List<LayoutDesignGeneratedImageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetGeneratedImages(int layoutDesignId)
        {
            try
            {
                var userId = GetRequiredUserId();
                var result = await _layoutDesignImageGenerationService.GetGeneratedImagesAsync(layoutDesignId, userId);

                return Ok(new ApiResponse<List<LayoutDesignGeneratedImageDto>>
                {
                    Success = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = $"Found {result.Count} generated images",
                    Payload = result
                });
            }
            catch (UnauthorizedException ex)
            {
                throw new UnauthorizedException(ex.Message);
            }
            catch (ForbiddenException ex)
            {
                throw new ForbiddenException(ex.Message);
            }
            catch (NotFoundException ex)
            {
                throw new NotFoundException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while listing generated images for LayoutDesign {LayoutDesignId}", layoutDesignId);
                throw new Exception("Failed to fetch generated images. Please try again.");
            }
        }

        /// <summary>
        /// Get all generated AI images of the authenticated user.
        /// </summary>
        [HttpGet("generated-images")]
        [Authorize]
        [ProducesResponseType(typeof(List<LayoutDesignGeneratedImageDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetAllGeneratedImagesByUserId()
        {
            try
            {
                var userId = GetRequiredUserId();
                var result = await _layoutDesignImageGenerationService.GetAllGeneratedImagesByUserIdAsync(userId);

                return Ok(new ApiResponse<List<LayoutDesignGeneratedImageDto>>
                {
                    Success = true,
                    StatusCode = StatusCodes.Status200OK,
                    Message = $"Found {result.Count} generated images",
                    Payload = result
                });
            }
            catch (UnauthorizedException ex)
            {
                throw new UnauthorizedException(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while listing generated images by user");
                throw new Exception("Failed to fetch generated images. Please try again.");
            }
        }

        private int GetRequiredUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedException("Unable to identify user from token");
            }

            return userId;
        }

        //private static string NormalizeBase64(string value)
        //{
        //    const string marker = "base64,";
        //    var markerIndex = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        //    if (markerIndex >= 0)
        //    {
        //        return value[(markerIndex + marker.Length)..].Trim();
        //    }

        //    return value.Trim();
        //}
    }

}
