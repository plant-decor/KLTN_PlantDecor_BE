using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;

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
        private readonly ILogger<RoomDesignController> _logger;

        public RoomDesignController(
            IRoomDesignService roomDesignService,
            ILogger<RoomDesignController> logger)
        {
            _roomDesignService = roomDesignService;
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
        /// Analyze room image and get plant recommendations
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
        /// <param name="request">Room design request with image and optional filters</param>
        /// <returns>Room analysis and plant recommendations</returns>
        /// <response code="200">Successfully analyzed room and generated recommendations</response>
        /// <response code="400">Invalid request (missing image, etc.)</response>
        /// <response code="500">AI processing error</response>
        [HttpPost("analyze")]
        [ProducesResponseType(typeof(RoomDesignResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AnalyzeAndRecommend([FromBody] RoomDesignRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.RoomImageBase64))
            {
                return BadRequest(new { success = false, message = "Room image is required" });
            }

            // Validate base64 image
            var normalizedImageBase64 = NormalizeBase64(request.RoomImageBase64);
            try
            {
                var imageBytes = Convert.FromBase64String(normalizedImageBase64);
                if (imageBytes.Length > 10 * 1024 * 1024) // 10MB limit
                {
                    return BadRequest(new { success = false, message = "Image size exceeds 10MB limit" });
                }
            }
            catch (FormatException)
            {
                return BadRequest(new { success = false, message = "Invalid base64 image format" });
            }

            try
            {
                request.RoomImageBase64 = normalizedImageBase64;
                var result = await _roomDesignService.AnalyzeAndRecommendAsync(request);

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = $"Found {result.TotalCount} plant recommendations"
                });
            }
            catch (BadRequestException ex)
            {
                _logger.LogWarning(ex, "Invalid room design request");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in room design analysis");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to analyze room. Please try again."
                });
            }
        }

        /// <summary>
        /// Analyze room image and get plant recommendations from multipart form-data upload.
        /// </summary>
        /// <param name="request">Multipart request containing image file and optional filters</param>
        /// <returns>Room analysis and plant recommendations</returns>
        [HttpPost("analyze-upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(RoomDesignResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AnalyzeAndRecommendUpload([FromForm] AnalyzeAndRecommendUploadRequest request)
        {
            try
            {
                var userId = GetOptionalUserId();
                var result = await _roomDesignService.AnalyzeAndRecommendUploadAsync(request, userId);

                return Ok(new
                {
                    success = true,
                    data = result,
                    message = $"Found {result.TotalCount} plant recommendations"
                });
            }
            catch (BadRequestException ex)
            {
                _logger.LogWarning(ex, "Invalid room design upload request");
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in room design analysis (multipart)");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to analyze room. Please try again."
                });
            }
        }

        private int? GetOptionalUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        /// <summary>
        /// Analyze room image only (without plant recommendations)
        /// </summary>
        /// <remarks>
        /// Quick analysis of room characteristics without searching for plants.
        /// Useful for previewing the analysis before getting recommendations.
        /// </remarks>
        /// <param name="request">Request containing a base64 encoded room image</param>
        /// <returns>Room analysis result</returns>
        [HttpPost("analyze-room-only")]
        [ProducesResponseType(typeof(RoomAnalysisDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> AnalyzeRoomOnly([FromBody] AnalyzeRoomOnlyRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ImageBase64))
            {
                return BadRequest(new { success = false, message = "Room image is required" });
            }

            var normalizedImageBase64 = NormalizeBase64(request.ImageBase64);
            try
            {
                var imageBytes = Convert.FromBase64String(normalizedImageBase64);
                if (imageBytes.Length > 10 * 1024 * 1024) // 10MB limit
                {
                    return BadRequest(new { success = false, message = "Image size exceeds 10MB limit" });
                }
            }
            catch (FormatException)
            {
                return BadRequest(new { success = false, message = "Invalid base64 image format" });
            }

            try
            {
                var result = await _roomDesignService.AnalyzeRoomAsync(normalizedImageBase64);

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing room");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to analyze room"
                });
            }
        }

        /// <summary>
        /// Analyze room image only from multipart form-data upload.
        /// </summary>
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
                return BadRequest(new { success = false, message = "Room image file is required" });
            }

            if (request.Image.Length > 10 * 1024 * 1024) // 10MB limit
            {
                return BadRequest(new { success = false, message = "Image size exceeds 10MB limit" });
            }

            try
            {
                await using var stream = request.Image.OpenReadStream();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var imageBase64 = Convert.ToBase64String(memoryStream.ToArray());

                var result = await _roomDesignService.AnalyzeRoomAsync(imageBase64);

                return Ok(new
                {
                    success = true,
                    data = result
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing room (multipart)");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to analyze room"
                });
            }
        }

        private static string NormalizeBase64(string value)
        {
            const string marker = "base64,";
            var markerIndex = value.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (markerIndex >= 0)
            {
                return value[(markerIndex + marker.Length)..].Trim();
            }

            return value.Trim();
        }
    }

}
