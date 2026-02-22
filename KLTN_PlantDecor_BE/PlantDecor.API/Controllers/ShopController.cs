using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;

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
                Message = "Lấy danh sách cây cho shop thành công",
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
                    Message = $"Không tìm thấy cây với ID {id}"
                });

            return Ok(new ApiResponse<PlantResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy chi tiết cây thành công",
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
            var availableInstances = instances.Where(i => i.Status == "Available").ToList();
            return Ok(new ApiResponse<List<PlantInstanceResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy danh sách instances có sẵn thành công",
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
                Message = "Lấy danh sách sản phẩm phụ kiện thành công",
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
                    Message = $"Không tìm thấy sản phẩm với ID {id}"
                });

            return Ok(new ApiResponse<InventoryResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Lấy chi tiết sản phẩm thành công",
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
                Message = "Lấy danh sách categories thành công",
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
                Message = "Lấy danh sách tags thành công",
                Payload = tags
            });
        }

        #endregion
    }
}
