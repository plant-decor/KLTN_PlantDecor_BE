using Microsoft.EntityFrameworkCore;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
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

        public async Task<List<TagResponseDto>> GetAllTagsAsync()
        {
            var cachedTags = await _cacheService.GetDataAsync<List<TagResponseDto>>(ALL_TAGS_KEY);
            if (cachedTags != null)
                return cachedTags;

            var tags = await _unitOfWork.TagRepository.GetAllAsync();
            var response = tags.OrderBy(t => t.TagName).ToResponseList();

            await _cacheService.SetDataAsync(ALL_TAGS_KEY, response, DateTimeOffset.Now.AddMinutes(30));
            return tags.OrderBy(t => t.TagName).ToResponseList();
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

                await _cacheService.RemoveDataAsync(ALL_TAGS_KEY);

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

                await _cacheService.RemoveDataAsync(ALL_TAGS_KEY);

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
                if (tag.Plants.Any() || tag.Inventories.Any() || tag.PlantCombos.Any())
                    throw new BadRequestException("Không thể xóa tag đang được gắn với sản phẩm. Vui lòng gỡ liên kết trước.");

                _unitOfWork.TagRepository.PrepareRemove(tag);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await _cacheService.RemoveDataAsync(ALL_TAGS_KEY);

                return true;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }
    }
}
