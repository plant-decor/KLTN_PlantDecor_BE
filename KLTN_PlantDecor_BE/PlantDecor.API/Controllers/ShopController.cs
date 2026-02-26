using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.API.Controllers
{
    /// <summary>
    /// API công khai cho Shop (Khách hàng)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ShopController : ControllerBase
    {
        private readonly IPlantService _plantService;
        private readonly IInventoryService _inventoryService;
        private readonly ICategoryService _categoryService;
        private readonly ITagService _tagService;
        private readonly IPlantInstanceService _plantInstanceService;
        private readonly IPlantComboService _plantComboService;

        public ShopController(
            IPlantService plantService,
            IInventoryService inventoryService,
            ICategoryService categoryService,
            ITagService tagService,
            IPlantInstanceService plantInstanceService,
            IPlantComboService plantComboService)
        {
            _plantService = plantService;
            _inventoryService = inventoryService;
            _categoryService = categoryService;
            _tagService = tagService;
            _plantInstanceService = plantInstanceService;
            _plantComboService = plantComboService;
        }

        #region Plants

        /// <summary>
        /// Lấy danh sách cây có sẵn để bán (có instance available)
        /// </summary>
        [HttpGet("plants")]
        public async Task<IActionResult> GetPlantsForShop([FromQuery] Pagination pagination)
        {
            var plants = await _plantService.GetPlantsForShopAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<PlantListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get plants for shop successfully",
                Payload = plants
            });
        }

        /// <summary>
        /// Lấy chi tiết cây theo ID
        /// </summary>
        [HttpGet("plants/{id}")]
        public async Task<IActionResult> GetPlantDetail(int id)
        {
            var plant = await _plantService.GetPlantByIdAsync(id);
            if (plant == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = $"Plant with ID {id} not found"
                });

            return Ok(new ApiResponse<PlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get plant detail successfully",
                Payload = plant
            });
        }

        /// <summary>
        /// Lấy các instance có sẵn của một cây
        /// </summary>
        [HttpGet("plants/{plantId}/instances")]
        public async Task<IActionResult> GetPlantInstances(int plantId, [FromQuery] Pagination pagination)
        {
            var instances = await _plantInstanceService.GetInstancesByPlantIdAsync(plantId, pagination);
            return Ok(new ApiResponse<PaginatedResult<PlantInstanceResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get available instances successfully",
                Payload = availableInstances
            });
        }

        #endregion

        #region Inventories

        /// <summary>
        /// Lấy danh sách sản phẩm phụ kiện có sẵn (có stock > 0)
        /// </summary>
        [HttpGet("inventories")]
        public async Task<IActionResult> GetInventoriesForShop([FromQuery] Pagination pagination)
        {
            var inventories = await _inventoryService.GetInventoriesForShopAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<InventoryListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get inventory items for shop successfully",
                Payload = inventories
            });
        }

        /// <summary>
        /// Lấy chi tiết sản phẩm phụ kiện theo ID
        /// </summary>
        [HttpGet("inventories/{id}")]
        public async Task<IActionResult> GetInventoryDetail(int id)
        {
            var inventory = await _inventoryService.GetInventoryByIdAsync(id);
            if (inventory == null)
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    StatusCode = StatusCodes.Status404NotFound,
                    Message = $"Inventory item with ID {id} not found"
                });

            return Ok(new ApiResponse<InventoryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get inventory item detail successfully",
                Payload = inventory
            });
        }

        #endregion

        #region Combos

        /// <summary>
        /// Lấy danh sách combo cho shop
        /// </summary>
        [HttpGet("combos")]
        public async Task<IActionResult> GetCombosForShop([FromQuery] Pagination pagination)
        {
            var combos = await _plantComboService.GetCombosForShopAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<PlantComboListResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách combos cho shop thành công",
                Payload = combos
            });
        }

        #endregion

        #region Categories & Tags

        /// <summary>
        /// Lấy tất cả categories (dạng cây)
        /// </summary>
        [HttpGet("categories")]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await _categoryService.GetRootCategoriesAsync();
            return Ok(new ApiResponse<IEnumerable<CategoryResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get all categories successfully",
                Payload = categories
            });
        }

        /// <summary>
        /// Lấy tất cả tags
        /// </summary>
        [HttpGet("tags")]
        public async Task<IActionResult> GetTags([FromQuery] Pagination pagination)
        {
            var tags = await _tagService.GetAllTagsAsync(pagination);
            return Ok(new ApiResponse<PaginatedResult<TagResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get all tags successfully",
                Payload = tags
            });
        }

        #endregion
    }
}
