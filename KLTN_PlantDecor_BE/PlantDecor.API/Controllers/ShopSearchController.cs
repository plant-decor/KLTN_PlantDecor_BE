using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.API.Controllers
{
    [Route("api/shop/search")]
    [ApiController]
    public class ShopSearchController : ControllerBase
    {
        private readonly IShopSearchService _shopSearchService;

        public ShopSearchController(IShopSearchService shopSearchService)
        {
            _shopSearchService = shopSearchService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Search([FromBody] ShopUnifiedSearchRequestDto request)
        {
            var result = await _shopSearchService.SearchAsync(request);
            return Ok(new ApiResponse<ShopUnifiedSearchResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Search shop products successfully",
                Payload = result
            });
        }
    }
}