using Microsoft.EntityFrameworkCore;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.DTOs.Updates;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
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

            var allCategories = await _unitOfWork.CategoryRepository.GetAllAsync();
            var result = BuildCategoryTree(allCategories, activeOnly: false);
            await _cacheService.SetDataAsync(ROOT_CATEGORIES_KEY, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<CategoryResponseDto> GetCategoryByIdAsync(int id)
        {
            var allCategories = await _unitOfWork.CategoryRepository.GetAllAsync();
            var tree = BuildCategoryTree(allCategories, activeOnly: false);
            var category = FindCategoryInTree(tree, id);
            if (category == null)
                throw new NotFoundException($"Category với ID {id} không tồn tại");

            return category;
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
            await _cacheService.RemoveByPrefixAsync("plants_system_search");
            await _cacheService.RemoveByPrefixAsync("plants_shop_search");
            await _cacheService.RemoveByPrefixAsync("materials_shop");
            await _cacheService.RemoveByPrefixAsync("combos_shop");
            await _cacheService.RemoveByPrefixAsync("common_plants_all");
            await _cacheService.RemoveByPrefixAsync("nursery_common_plants");
            await _cacheService.RemoveByPrefixAsync("shop_unified_search");
        }

        public Task<List<CategoryResponseDto>> GetAllActiveCategoriesAsync()
        {
            throw new NotImplementedException();
        }

        public async Task<List<CategoryResponseDto>> GetCategoriesByTypeAsync(int categoryType, bool activeOnly = true)
        {
            if (!Enum.IsDefined(typeof(CategoryTypeEnum), categoryType))
                throw new BadRequestException($"CategoryType {categoryType} is invalid");

            var categories = await _unitOfWork.CategoryRepository.GetByCategoryTypeAsync(categoryType, activeOnly);
            return categories.Select(c => c.ToResponse()).ToList();
        }

        public async Task<List<CategoryResponseDto>> GetRootActiveCategoriesAsync()
        {
            var cachedData = await _cacheService.GetDataAsync<List<CategoryResponseDto>>(ROOT_ACTIVE_CATEGORIES_KEY);
            if (cachedData != null)
            {
                return cachedData;
            }

            var allCategories = await _unitOfWork.CategoryRepository.GetAllAsync();
            var result = BuildCategoryTree(allCategories, activeOnly: true);
            await _cacheService.SetDataAsync(ROOT_ACTIVE_CATEGORIES_KEY, result, DateTimeOffset.Now.AddMinutes(30));

            return result;
        }

        private static List<CategoryResponseDto> BuildCategoryTree(IEnumerable<Category> categories, bool activeOnly)
        {
            var source = activeOnly
                ? categories.Where(c => c.IsActive == true).ToList()
                : categories.ToList();

            var dtoMap = source.ToDictionary(c => c.Id, c => new CategoryResponseDto
            {
                Id = c.Id,
                ParentCategoryId = c.ParentCategoryId,
                Name = c.Name,
                IsActive = c.IsActive,
                CategoryType = c.CategoryType,
                CategoryTypeName = ((CategoryTypeEnum)c.CategoryType).ToString(),
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                SubCategories = new List<CategoryResponseDto>()
            });

            foreach (var item in source)
            {
                var currentDto = dtoMap[item.Id];

                if (item.ParentCategoryId.HasValue && dtoMap.TryGetValue(item.ParentCategoryId.Value, out var parentDto))
                {
                    currentDto.ParentCategoryName = parentDto.Name;
                    parentDto.SubCategories.Add(currentDto);
                }
            }

            SortTreeByName(dtoMap.Values);

            return dtoMap.Values
                .Where(dto => !dto.ParentCategoryId.HasValue || !dtoMap.ContainsKey(dto.ParentCategoryId.Value))
                .OrderBy(dto => dto.Name)
                .ToList();
        }

        private static void SortTreeByName(IEnumerable<CategoryResponseDto> nodes)
        {
            foreach (var node in nodes)
            {
                node.SubCategories = node.SubCategories
                    .OrderBy(c => c.Name)
                    .ToList();

                SortTreeByName(node.SubCategories);
            }
        }

        private static CategoryResponseDto? FindCategoryInTree(IEnumerable<CategoryResponseDto> nodes, int id)
        {
            foreach (var node in nodes)
            {
                if (node.Id == id)
                {
                    return node;
                }

                var found = FindCategoryInTree(node.SubCategories, id);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
    }
}
