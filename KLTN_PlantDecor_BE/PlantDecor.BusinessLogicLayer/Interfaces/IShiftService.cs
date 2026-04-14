using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IShiftService
    {
        Task<List<ShiftResponseDto>> GetAllAsync();
    }
}
