using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlantDecor.API.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;

namespace PlantDecor.API.Controllers
{
    [ApiController]
    [Route("api/admin/deposit-policies")]
    [Authorize(Roles = "Admin")]
    public class DepositPoliciesController : ControllerBase
    {
        private readonly IDepositPolicyService _depositPolicyService;

        public DepositPoliciesController(IDepositPolicyService depositPolicyService)
        {
            _depositPolicyService = depositPolicyService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _depositPolicyService.GetAllAsync();
            return Ok(new ApiResponse<List<DepositPolicyResponseDto>>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get deposit policies successfully",
                Payload = result
            });
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _depositPolicyService.GetByIdAsync(id);
            return Ok(new ApiResponse<DepositPolicyResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Get deposit policy successfully",
                Payload = result
            });
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] DepositPolicyRequestDto request)
        {
            var result = await _depositPolicyService.CreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, new ApiResponse<DepositPolicyResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status201Created,
                Message = "Deposit policy created successfully",
                Payload = result
            });
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateDepositPolicyRequestDto request)
        {
            var result = await _depositPolicyService.UpdateAsync(id, request);
            return Ok(new ApiResponse<DepositPolicyResponseDto>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Deposit policy updated successfully",
                Payload = result
            });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _depositPolicyService.DeleteAsync(id);
            return Ok(new ApiResponse<object>
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                Message = "Deposit policy deleted successfully",
                Payload = null
            });
        }
    }
}
