using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;

namespace PlantDecor.BusinessLogicLayer.Interfaces
{
    public interface IShopSearchService
    {
        Task<ShopUnifiedSearchResponseDto> SearchAsync(ShopUnifiedSearchRequestDto request);
    }
}