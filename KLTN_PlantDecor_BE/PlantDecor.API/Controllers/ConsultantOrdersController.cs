using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Helpers;

namespace PlantDecor.API.Controllers
{
    [Route("api/consultant/orders")]
    [ApiController]
    [Authorize(Roles = "Consultant")]
    public class ConsultantOrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public ConsultantOrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        /// <summary>
        /// [Consultant] Get order list with paging and filters.
        /// </summary>
        [HttpPost("search")]
        public async Task<IActionResult> GetOrders([FromBody] ConsultantOrderSearchRequestDto request)
        {
            var filter = request ?? new ConsultantOrderSearchRequestDto();
            var pagination = filter.Pagination ?? new Pagination();
            var result = await _orderService.GetOrdersForConsultantAsync(filter, pagination);

            return Ok(new ApiResponse<PaginatedResult<OrderResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get orders successfully",
                Payload = result
            });
        }

        /// <summary>
        /// [Consultant] Get order detail by id.
        /// </summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetOrderDetail([FromRoute] int id)
        {
            var result = await _orderService.GetOrderByIdForConsultantAsync(id);
            return Ok(new ApiResponse<OrderResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get order successfully",
                Payload = result
            });
        }
    }
}
