using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class WishlistService : IWishlistService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string ALL_WISHLISTS_KEY = "wishlist_user";

        public WishlistService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<PaginatedResult<WishlistItemResponseDto>> GetWishlistByUserIdAsync(int userId, Pagination pagination)
        {
            var cacheKey = $"{ALL_WISHLISTS_KEY}_{userId}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cached = await _cacheService.GetDataAsync<PaginatedResult<WishlistItemResponseDto>>(cacheKey);
            if (cached != null)
                return cached;

            var paginatedEntities = await _unitOfWork.WishlistRepository.GetByUserIdWithPaginationAsync(userId, pagination);
            var result = new PaginatedResult<WishlistItemResponseDto>(
                paginatedEntities.Items.ToResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize);

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<WishlistItemResponseDto> AddToWishlistAsync(int userId, int plantId)
        {
            var plant = await _unitOfWork.PlantRepository.GetByIdAsync(plantId);
            if (plant == null)
                throw new NotFoundException($"Plant with ID {plantId} not exists");

            if (await _unitOfWork.WishlistRepository.ExistsAsync(userId, plantId))
                throw new BadRequestException("Plant existed already");

            var wishlist = new Wishlist
            {
                UserId = userId,
                PlantId = plantId,
                CreatedAt = DateTime.Now
            };
            await _unitOfWork.WishlistRepository.CreateAsync(wishlist);

            await _cacheService.RemoveByPrefixAsync($"{ALL_WISHLISTS_KEY}_{userId}");

            var created = await _unitOfWork.WishlistRepository.GetByUserAndPlantAsync(userId, plantId);
            return created!.ToResponse();
        }

        public async Task<bool> RemoveFromWishlistAsync(int userId, int plantId)
        {
            var item = await _unitOfWork.WishlistRepository.GetByUserAndPlantAsync(userId, plantId);
            if (item == null)
                throw new NotFoundException("Plant doesn't exist in wishlist");

            var result = await _unitOfWork.WishlistRepository.RemoveAsync(item);
            await _cacheService.RemoveByPrefixAsync($"{ALL_WISHLISTS_KEY}_{userId}");
            return result;
        }

        public async Task<bool> IsInWishlistAsync(int userId, int plantId)
        {
            return await _unitOfWork.WishlistRepository.ExistsAsync(userId, plantId);
        }
    }
}
