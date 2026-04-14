using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class ShiftService : IShiftService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string CACHE_KEY = "shifts_all";

        public ShiftService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<List<ShiftResponseDto>> GetAllAsync()
        {
            var cached = await _cacheService.GetDataAsync<List<ShiftResponseDto>>(CACHE_KEY);
            if (cached != null) return cached;

            var shifts = await _unitOfWork.ShiftRepository.GetAllAsync();
            var result = shifts.Select(s => new ShiftResponseDto
            {
                Id = s.Id,
                ShiftName = s.ShiftName,
                StartTime = s.StartTime,
                EndTime = s.EndTime
            }).ToList();

            await _cacheService.SetDataAsync(CACHE_KEY, result, DateTimeOffset.Now.AddHours(24));
            return result;
        }
    }
}
