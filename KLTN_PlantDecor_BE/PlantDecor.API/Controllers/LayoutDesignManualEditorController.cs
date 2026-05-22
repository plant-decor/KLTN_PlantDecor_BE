using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using System.Security.Claims;

namespace PlantDecor.API.Controllers
{
    [Route("api/layout-designs")]
    [ApiController]
    [Authorize(Roles = "Customer")]
    public class LayoutDesignManualEditorController : ControllerBase
    {
        private readonly ILayoutDesignManualEditorService _manualEditorService;
        private readonly ILogger<LayoutDesignManualEditorController> _logger;

        public LayoutDesignManualEditorController(
            ILayoutDesignManualEditorService manualEditorService,
            ILogger<LayoutDesignManualEditorController> logger)
        {
            _manualEditorService = manualEditorService;
            _logger = logger;
        }

        [HttpGet("{layoutDesignId:int}/manual-editor")]
        [ProducesResponseType(typeof(LayoutDesignManualEditorContextDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetManualEditorContext(int layoutDesignId)
        {
            var userId = GetRequiredUserId();
            var result = await _manualEditorService.GetEditorContextAsync(layoutDesignId, userId);

            return Ok(new ApiResponse<LayoutDesignManualEditorContextDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Fetched manual editor context successfully",
                Payload = result
            });
        }

        [HttpPut("{layoutDesignId:int}/manual-editor/draft")]
        [ProducesResponseType(typeof(LayoutDesignManualEditorImageDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> SaveCompositeDraft(
            int layoutDesignId,
            [FromBody] LayoutDesignManualDraftRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.LayerJson))
            {
                throw new BadRequestException("LayerJson is required");
            }

            var userId = GetRequiredUserId();
            var result = await _manualEditorService.SaveCompositeDraftAsync(layoutDesignId, userId, request.LayerJson);

            return Ok(new ApiResponse<LayoutDesignManualEditorImageDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Saved composite draft successfully",
                Payload = result
            });
        }

        [HttpPut("{layoutDesignId:int}/plants/{layoutDesignPlantId:int}/manual-editor/draft")]
        [ProducesResponseType(typeof(LayoutDesignManualEditorImageDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> SavePlantDraft(
            int layoutDesignId,
            int layoutDesignPlantId,
            [FromBody] LayoutDesignManualDraftRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.LayerJson))
            {
                throw new BadRequestException("LayerJson is required");
            }

            var userId = GetRequiredUserId();
            var result = await _manualEditorService.SavePlantDraftAsync(layoutDesignId, layoutDesignPlantId, userId, request.LayerJson);

            return Ok(new ApiResponse<LayoutDesignManualEditorImageDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Saved plant draft successfully",
                Payload = result
            });
        }

        [HttpPost("{layoutDesignId:int}/manual-editor/publish")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(LayoutDesignManualEditorImageDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> PublishComposite(
            int layoutDesignId,
            [FromForm] LayoutDesignManualPublishRequestDto request)
        {
            if (request == null || request.Image == null)
            {
                throw new BadRequestException("Manual image file is required");
            }

            var userId = GetRequiredUserId();
            var result = await _manualEditorService.PublishCompositeAsync(layoutDesignId, userId, request.Image, request.LayerJson);

            return Ok(new ApiResponse<LayoutDesignManualEditorImageDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Published composite image successfully",
                Payload = result
            });
        }

        [HttpPost("{layoutDesignId:int}/plants/{layoutDesignPlantId:int}/manual-editor/publish")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(LayoutDesignManualEditorImageDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> PublishPlant(
            int layoutDesignId,
            int layoutDesignPlantId,
            [FromForm] LayoutDesignManualPublishRequestDto request)
        {
            if (request == null || request.Image == null)
            {
                throw new BadRequestException("Manual image file is required");
            }

            var userId = GetRequiredUserId();
            var result = await _manualEditorService.PublishPlantAsync(layoutDesignId, layoutDesignPlantId, userId, request.Image, request.LayerJson);

            return Ok(new ApiResponse<LayoutDesignManualEditorImageDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Published plant image successfully",
                Payload = result
            });
        }

        [HttpPost("{layoutDesignId:int}/manual-editor/beautifyImage")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(LayoutDesignManualEditorImageDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> BeautifyComposite(
            int layoutDesignId,
            [FromForm] LayoutDesignManualBeautifyRequestDto request)
        {
            if (request == null || ((request.Image == null || request.Image.Length == 0) && string.IsNullOrWhiteSpace(request.ImageUrl)))
            {
                throw new BadRequestException("Image file or ImageUrl is required");
            }

            var userId = GetRequiredUserId();
            var result = await _manualEditorService.BeautifyCompositeAsync(layoutDesignId, userId, request.Image, request.ImageUrl, request.LayerJson);

            return Ok(new ApiResponse<LayoutDesignManualEditorImageDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Beautified composite image successfully",
                Payload = result
            });
        }

        [HttpPost("{layoutDesignId:int}/manual-editor/beautify")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(LayoutDesignManualEditorImageDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> BeautifyCompositeJson(
            int layoutDesignId,
            [FromBody] LayoutDesignManualBeautifyRequestDto request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.ImageUrl))
            {
                throw new BadRequestException("ImageUrl is required");
            }

            var userId = GetRequiredUserId();
            var result = await _manualEditorService.BeautifyCompositeAsync(layoutDesignId, userId, null, request.ImageUrl, request.LayerJson);

            return Ok(new ApiResponse<LayoutDesignManualEditorImageDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Beautified composite image successfully",
                Payload = result
            });
        }

        [HttpPost("{layoutDesignId:int}/manual-editor/calculate-total")]
        [ProducesResponseType(typeof(LayoutDesignManualCalculateResponseDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> CalculateManualTotal(
            int layoutDesignId,
            [FromBody] LayoutDesignManualCalculateRequestDto request)
        {
            if (request == null || request.Items == null || request.Items.Count == 0)
            {
                throw new BadRequestException("Items are required");
            }

            var userId = GetRequiredUserId();
            var result = await _manualEditorService.CalculateManualTotalAsync(layoutDesignId, userId, request);

            return Ok(new ApiResponse<LayoutDesignManualCalculateResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Calculated manual total successfully",
                Payload = result
            });
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
    }
}
