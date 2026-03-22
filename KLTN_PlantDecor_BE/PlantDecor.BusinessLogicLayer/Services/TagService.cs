using Microsoft.EntityFrameworkCore;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class TagService : ITagService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string ALL_TAGS_KEY = "tags_all";
        public TagService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<PaginatedResult<TagResponseDto>> GetAllTagsAsync(Pagination pagination)
        {
            var cacheKey = $"{ALL_TAGS_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedTags = await _cacheService.GetDataAsync<PaginatedResult<TagResponseDto>>(cacheKey);
            if (cachedTags != null)
                return cachedTags;

            var paginatedEntities = await _unitOfWork.TagRepository.GetAllTagsWithPaginationAsync(pagination);
            var result = new PaginatedResult<TagResponseDto>(
                paginatedEntities.Items.ToResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<TagResponseDto?> GetTagByIdAsync(int id)
        {
            var tag = await _unitOfWork.TagRepository.GetByIdAsync(id);
            if (tag == null)
                return null;

            return tag.ToResponse();
        }

        public async Task<TagResponseDto> CreateTagAsync(TagRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Check if tag name already exists
                if (await _unitOfWork.TagRepository.ExistsByNameAsync(request.TagName))
                    throw new BadRequestException($"Tag với tên '{request.TagName}' đã tồn tại");

                var tag = request.ToEntity();

                _unitOfWork.TagRepository.PrepareCreate(tag);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return tag.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<TagResponseDto> UpdateTagAsync(int id, TagUpdateDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var tag = await _unitOfWork.TagRepository.GetByIdAsync(id);
                if (tag == null)
                    throw new NotFoundException($"Tag với ID {id} không tồn tại");

                // Check if tag name already exists (excluding current tag)
                if (request.TagName != null && await _unitOfWork.TagRepository.ExistsByNameAsync(request.TagName, id))
                    throw new BadRequestException($"Tag với tên '{request.TagName}' đã tồn tại");

                request.ToUpdate(tag);

                _unitOfWork.TagRepository.PrepareUpdate(tag);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return tag.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> DeleteTagAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var tag = await _unitOfWork.TagRepository.GetByIdWithProductsAsync(id);
                if (tag == null)
                    throw new NotFoundException($"Tag với ID {id} không tồn tại");

                // Check if tag is assigned to any products
                if (tag.Plants.Any() || tag.Materials.Any() || tag.PlantCombos.Any())
                    throw new BadRequestException("Không thể xóa tag đang được gắn với sản phẩm. Vui lòng gỡ liên kết trước.");

                _unitOfWork.TagRepository.PrepareRemove(tag);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return true;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        private async Task InvalidateCacheAsync()
        {
            await _cacheService.RemoveByPrefixAsync(ALL_TAGS_KEY);
            await _cacheService.RemoveByPrefixAsync("plants_system_search");
            await _cacheService.RemoveByPrefixAsync("plants_shop_search");
            await _cacheService.RemoveByPrefixAsync("materials_shop");
            await _cacheService.RemoveByPrefixAsync("combos_shop");
            await _cacheService.RemoveByPrefixAsync("common_plants_all");
            await _cacheService.RemoveByPrefixAsync("nursery_common_plants");
        }
    }
}
