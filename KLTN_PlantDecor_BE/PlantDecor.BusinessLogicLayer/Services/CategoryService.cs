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
    public class CategoryService : ICategoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string ALL_CATEGORIES_KEY = "categories_all_tree";
        private const string ROOT_ACTIVE_CATEGORIES_KEY = "categories_active_roots";
        private const string ROOT_CATEGORIES_KEY = "categories_roots";

        public CategoryService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<PaginatedResult<CategoryResponseDto>> GetAllCategoriesAsync(Pagination pagination)
        {
            var cacheKey = $"{ALL_CATEGORIES_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<CategoryResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.CategoryRepository.GetAllWithParentAsync(pagination);
            var result = new PaginatedResult<CategoryResponseDto>(
                paginatedEntities.Items.ToResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));

            return result;
        }

        public async Task<List<CategoryResponseDto>> GetRootCategoriesAsync()
        {
            var cachedData = await _cacheService.GetDataAsync<List<CategoryResponseDto>>(ROOT_CATEGORIES_KEY);
            if (cachedData != null)
                return cachedData;
            var rootCategories = await _unitOfWork.CategoryRepository.GetRootCategoriesWithChildrenAsync();
            var result = rootCategories.ToResponseListWithChildren();
            await _cacheService.SetDataAsync(ROOT_CATEGORIES_KEY, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<CategoryResponseDto?> GetCategoryByIdAsync(int id)
        {
            var category = await _unitOfWork.CategoryRepository.GetByIdWithDetailsAsync(id);
            if (category == null)
                return null;

            return category.ToResponseWithChildren();
        }

        public async Task<CategoryResponseDto> CreateCategoryAsync(CategoryRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if(request.ParentCategoryId.HasValue)
                {
                    var parent = await _unitOfWork.CategoryRepository.GetByIdAsync(request.ParentCategoryId.Value);
                    if (parent == null)
                        throw new NotFoundException($"Category cha với ID {request.ParentCategoryId} không tồn tại");
                }

                if(await _unitOfWork.CategoryRepository.ExistsByNameAsync(request.Name))
                    throw new BadRequestException($"Category với tên '{request.Name}' đã tồn tại");

                var category = request.ToEntity();
                _unitOfWork.CategoryRepository.PrepareCreate(category);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return category.ToResponse();
            }
            catch(Exception) {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<CategoryResponseDto> UpdateCategoryAsync(int id, CategoryUpdateDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var category = await _unitOfWork.CategoryRepository.GetByIdWithDetailsAsync(id);
                if (category == null)
                    throw new NotFoundException($"Category với ID {id} không tồn tại");

                // Validate parent category if provided
                if (request.ParentCategoryId.HasValue)
                {
                    if (request.ParentCategoryId.Value == id)
                        throw new BadRequestException("Category không thể là cha của chính nó");

                    var parent = await _unitOfWork.CategoryRepository.GetByIdAsync(request.ParentCategoryId.Value);
                    if (parent == null)
                        throw new NotFoundException($"Category cha với ID {request.ParentCategoryId} không tồn tại");
                }

                // Check if category name already exists (excluding current category)
                if (request.Name != null && await _unitOfWork.CategoryRepository.ExistsByNameAsync(request.Name, id))
                    throw new BadRequestException($"Category với tên '{request.Name}' đã tồn tại");

                request.ToUpdate(category);

                _unitOfWork.CategoryRepository.PrepareUpdate(category);
                await _unitOfWork.SaveAsync();

                await _unitOfWork.CommitTransactionAsync();
                await InvalidateCacheAsync();

                return category.ToResponse();
            }
            catch(Exception) {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var category = await _unitOfWork.CategoryRepository.GetByIdAsync(id);
                if (category == null)
                    throw new NotFoundException($"Category với ID {id} không tồn tại");

                // Check if category has children
                if (await _unitOfWork.CategoryRepository.HasChildrenAsync(id))
                    throw new BadRequestException("Không thể xóa category có category con. Vui lòng xóa category con trước.");

                // Check if category is assigned to plants or inventories
                if (await _unitOfWork.CategoryRepository.HasProductsAsync(id))
                    throw new BadRequestException("Không thể xóa category đang được gắn với sản phẩm. Vui lòng gỡ liên kết trước.");

                category.IsActive = false; // Soft delete by deactivating
                category.UpdatedAt = DateTime.Now;

                _unitOfWork.CategoryRepository.PrepareUpdate(category);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return true;
            }
            catch(Exception) {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var category = await _unitOfWork.CategoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new NotFoundException($"Category với ID {id} không tồn tại");

            category.IsActive = !category.IsActive;
            category.UpdatedAt = DateTime.Now;

            _unitOfWork.CategoryRepository.PrepareUpdate(category);
            await _unitOfWork.SaveAsync();
            await InvalidateCacheAsync();

            return category.IsActive ?? true;
        }

        private async Task InvalidateCacheAsync()
        {
            await _cacheService.RemoveByPrefixAsync(ALL_CATEGORIES_KEY);
            await _cacheService.RemoveDataAsync(ROOT_CATEGORIES_KEY);
            await _cacheService.RemoveDataAsync(ROOT_ACTIVE_CATEGORIES_KEY);
        }

        public Task<List<CategoryResponseDto>> GetAllActiveCategoriesAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<List<CategoryResponseDto>> GetRootActiveCategoriesAsync()
        {
            var cachedData = await _cacheService.GetDataAsync<List<CategoryResponseDto>>(ROOT_ACTIVE_CATEGORIES_KEY);
            if (cachedData != null)
            {
                return cachedData;
            }

            var rootActiveCategories = await _unitOfWork.CategoryRepository.GetRootActiveCategoriesWithChildrenAsync();

            var result = rootActiveCategories.ToResponseListWithChildren();
            await _cacheService.SetDataAsync(ROOT_ACTIVE_CATEGORIES_KEY, result, DateTimeOffset.Now.AddMinutes(30));

            return result;
        }
    }
}
