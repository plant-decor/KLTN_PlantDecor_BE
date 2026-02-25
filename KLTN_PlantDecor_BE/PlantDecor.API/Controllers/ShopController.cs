using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Enums;

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

        public ShopController(
            IPlantService plantService,
            IInventoryService inventoryService,
            ICategoryService categoryService,
            ITagService tagService,
            IPlantInstanceService plantInstanceService)
        {
            _plantService = plantService;
            _inventoryService = inventoryService;
            _categoryService = categoryService;
            _tagService = tagService;
            _plantInstanceService = plantInstanceService;
        }

        #region Plants

        /// <summary>
        /// Lấy danh sách cây có sẵn để bán (có instance available)
        /// </summary>
        [HttpGet("plants")]
        public async Task<IActionResult> GetPlantsForShop()
        {
            var plants = await _plantService.GetPlantsForShopAsync();
            return Ok(new ApiResponse<IEnumerable<PlantListResponseDto>>
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
        public async Task<IActionResult> GetPlantInstances(int plantId)
        {
            var instances = await _plantInstanceService.GetInstancesByPlantIdAsync(plantId);
            // Filter only available instances for shop
            var availableInstances = instances.Where(i => i.Status == (int)PlantInstanceStatusEnum.Available).ToList();
            return Ok(new ApiResponse<List<PlantInstanceResponseDto>>
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
        public async Task<IActionResult> GetInventoriesForShop()
        {
            var inventories = await _inventoryService.GetInventoriesForShopAsync();
            return Ok(new ApiResponse<IEnumerable<InventoryListResponseDto>>
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
        public async Task<IActionResult> GetTags()
        {
            var tags = await _tagService.GetAllTagsAsync();
            return Ok(new ApiResponse<IEnumerable<TagResponseDto>>
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
