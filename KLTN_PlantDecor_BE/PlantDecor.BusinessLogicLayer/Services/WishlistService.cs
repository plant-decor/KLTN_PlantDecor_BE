using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
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

        public async Task<WishlistItemResponseDto> AddToWishlistAsync(int userId, WishlistItemType itemType, int itemId)
        {
            // Validate item exists based on type
            await ValidateItemExistsAsync(itemType, itemId);

            if (await _unitOfWork.WishlistRepository.ExistsAsync(userId, itemType, itemId))
                throw new BadRequestException($"{itemType} already existed in wishlist");

            var wishlist = new Wishlist
            {
                UserId = userId,
                ItemType = itemType,
                IsDeleted = false,
                CreatedAt = DateTime.Now
            };

            // Set the appropriate foreign key based on item type
            switch (itemType)
            {
                case WishlistItemType.CommonPlant:
                    wishlist.CommonPlantId = itemId;
                    break;
                case WishlistItemType.PlantInstance:
                    wishlist.PlantInstanceId = itemId;
                    break;
                case WishlistItemType.NurseryPlantCombo:
                    wishlist.NurseryPlantComboId = itemId;
                    break;
                case WishlistItemType.NurseryMaterial:
                    wishlist.NurseryMaterialId = itemId;
                    break;
                default:
                    throw new BadRequestException($"Invalid item type: {itemType}");
            }

            await _unitOfWork.WishlistRepository.CreateAsync(wishlist);
            await _cacheService.RemoveByPrefixAsync($"{ALL_WISHLISTS_KEY}_{userId}");

            var created = await _unitOfWork.WishlistRepository.GetByUserAndItemAsync(userId, itemType, itemId);
            return created!.ToResponse();
        }

        public async Task<bool> RemoveFromWishlistAsync(int userId, WishlistItemType itemType, int itemId)
        {
            var item = await _unitOfWork.WishlistRepository.GetByUserAndItemAsync(userId, itemType, itemId);
            if (item == null)
                throw new NotFoundException($"{itemType} doesn't exist in wishlist");

            var result = await _unitOfWork.WishlistRepository.RemoveAsync(item);
            await _cacheService.RemoveByPrefixAsync($"{ALL_WISHLISTS_KEY}_{userId}");
            return result;
        }

        public async Task<bool> IsInWishlistAsync(int userId, WishlistItemType itemType, int itemId)
        {
            return await _unitOfWork.WishlistRepository.ExistsAsync(userId, itemType, itemId);
        }

        private async Task ValidateItemExistsAsync(WishlistItemType itemType, int itemId)
        {
            switch (itemType)
            {
                case WishlistItemType.CommonPlant:
                    var commonPlant = await _unitOfWork.CommonPlantRepository.GetByIdAsync(itemId);
                    if (commonPlant == null)
                        throw new NotFoundException($"CommonPlant with ID {itemId} not exists");
                    break;

                case WishlistItemType.PlantInstance:
                    var plantInstance = await _unitOfWork.PlantInstanceRepository.GetByIdAsync(itemId);
                    if (plantInstance == null)
                        throw new NotFoundException($"PlantInstance with ID {itemId} not exists");
                    break;

                case WishlistItemType.NurseryPlantCombo:
                    var nurseryPlantCombo = await _unitOfWork.NurseryPlantComboRepository.GetByIdAsync(itemId);
                    if (nurseryPlantCombo == null)
                        throw new NotFoundException($"NurseryPlantCombo with ID {itemId} not exists");
                    break;

                case WishlistItemType.NurseryMaterial:
                    var nurseryMaterial = await _unitOfWork.NurseryMaterialRepository.GetByIdAsync(itemId);
                    if (nurseryMaterial == null)
                        throw new NotFoundException($"NurseryMaterial with ID {itemId} not exists");
                    break;

                default:
                    throw new BadRequestException($"Invalid item type: {itemType}");
            }
        }
    }
}
