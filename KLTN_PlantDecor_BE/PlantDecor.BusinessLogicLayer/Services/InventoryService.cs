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
    public class InventoryService : IInventoryService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string ALL_INVENTORIES_KEY = "inventories_all";
        private const string ACTIVE_INVENTORIES_KEY = "inventories_active";
        private const string SHOP_INVENTORIES_KEY = "inventories_shop";

        public InventoryService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        #region CRUD Operations

        public async Task<PaginatedResult<InventoryListResponseDto>> GetAllInventoriesAsync(Pagination pagination)
        {
            var cacheKey = $"{ALL_INVENTORIES_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<InventoryListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.InventoryRepository.GetAllWithDetailsAsync(pagination);
            var result = new PaginatedResult<InventoryListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<PaginatedResult<InventoryListResponseDto>> GetActiveInventoriesAsync(Pagination pagination)
        {
            var cacheKey = $"{ACTIVE_INVENTORIES_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<InventoryListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.InventoryRepository.GetActiveWithDetailsAsync(pagination);
            var result = new PaginatedResult<InventoryListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<InventoryResponseDto?> GetInventoryByIdAsync(int id)
        {
            var inventory = await _unitOfWork.InventoryRepository.GetByIdWithDetailsAsync(id);
            if (inventory == null)
                return null;

            return inventory.ToResponse();
        }

        public async Task<InventoryResponseDto> CreateInventoryAsync(InventoryRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Check if inventory code already exists
                if (!string.IsNullOrEmpty(request.InventoryCode))
                {
                    if (await _unitOfWork.InventoryRepository.ExistsByCodeAsync(request.InventoryCode))
                        throw new BadRequestException($"Inventory với mã '{request.InventoryCode}' đã tồn tại");
                }

                var inventory = request.ToEntity();
                inventory.InventoryCode ??= InventoryMapper.GenerateInventoryCode();

                _unitOfWork.InventoryRepository.PrepareCreate(inventory);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return inventory.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<InventoryResponseDto> UpdateInventoryAsync(int id, InventoryUpdateDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var inventory = await _unitOfWork.InventoryRepository.GetByIdWithDetailsAsync(id);
                if (inventory == null)
                    throw new NotFoundException($"Inventory với ID {id} không tồn tại");

                // Check if inventory code already exists (excluding current inventory)
                if (!string.IsNullOrEmpty(request.InventoryCode))
                {
                    if (await _unitOfWork.InventoryRepository.ExistsByCodeAsync(request.InventoryCode, id))
                        throw new BadRequestException($"Inventory với mã '{request.InventoryCode}' đã tồn tại");
                }

                request.ToUpdate(inventory);

                _unitOfWork.InventoryRepository.PrepareUpdate(inventory);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return inventory.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> DeleteInventoryAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var inventory = await _unitOfWork.InventoryRepository.GetByIdWithOrdersAsync(id);
                if (inventory == null)
                    throw new NotFoundException($"Inventory với ID {id} không tồn tại");

                // Check if inventory is in cart or orders
                if (inventory.CartItems.Any())
                    throw new BadRequestException("Không thể xóa sản phẩm đang có trong giỏ hàng.");

                if (inventory.OrderItems.Any())
                    throw new BadRequestException("Không thể xóa sản phẩm đã có trong đơn hàng. Vui lòng vô hiệu hóa thay vì xóa.");

                // Soft delete by deactivating
                inventory.IsActive = false;
                inventory.UpdatedAt = DateTime.Now;

                _unitOfWork.InventoryRepository.PrepareUpdate(inventory);
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

        public async Task<bool> ToggleActiveAsync(int id)
        {
            var inventory = await _unitOfWork.InventoryRepository.GetByIdAsync(id);
            if (inventory == null)
                throw new NotFoundException($"Inventory với ID {id} không tồn tại");

            inventory.IsActive = !inventory.IsActive;
            inventory.UpdatedAt = DateTime.Now;

            _unitOfWork.InventoryRepository.PrepareUpdate(inventory);
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync();

            return inventory.IsActive ?? true;
        }

        #endregion

        #region Category & Tag Assignment

        public async Task<InventoryResponseDto> AssignCategoriesToInventoryAsync(AssignInventoryCategoriesDto request)
        {
            var inventory = await _unitOfWork.InventoryRepository.GetByIdWithDetailsAsync(request.InventoryId);
            if (inventory == null)
                throw new NotFoundException($"Inventory với ID {request.InventoryId} không tồn tại");

            // Get valid categories
            var categories = await _unitOfWork.CategoryRepository.GetAllAsync();
            var validCategories = categories.Where(c => request.CategoryIds.Contains(c.Id)).ToList();

            if (validCategories.Count != request.CategoryIds.Count)
            {
                var invalidIds = request.CategoryIds.Except(validCategories.Select(c => c.Id));
                throw new NotFoundException($"Các Category với ID {string.Join(", ", invalidIds)} không tồn tại");
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Clear existing categories and add new ones
                inventory.Categories.Clear();
                foreach (var category in validCategories)
                {
                    inventory.Categories.Add(category);
                }

                inventory.UpdatedAt = DateTime.Now;
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return inventory.ToResponse();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<InventoryResponseDto> AssignTagsToInventoryAsync(AssignInventoryTagsDto request)
        {
            var inventory = await _unitOfWork.InventoryRepository.GetByIdWithDetailsAsync(request.InventoryId);
            if (inventory == null)
                throw new NotFoundException($"Inventory với ID {request.InventoryId} không tồn tại");

            // Get valid tags
            var tags = await _unitOfWork.TagRepository.GetByIdsAsync(request.TagIds);

            if (tags.Count != request.TagIds.Count)
            {
                var invalidIds = request.TagIds.Except(tags.Select(t => t.Id));
                throw new NotFoundException($"Các Tag với ID {string.Join(", ", invalidIds)} không tồn tại");
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                // Clear existing tags and add new ones
                inventory.Tags.Clear();
                foreach (var tag in tags)
                {
                    inventory.Tags.Add(tag);
                }

                inventory.UpdatedAt = DateTime.Now;
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return inventory.ToResponse();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<InventoryResponseDto> RemoveCategoryFromInventoryAsync(int inventoryId, int categoryId)
        {
            var inventory = await _unitOfWork.InventoryRepository.GetByIdWithDetailsAsync(inventoryId);
            if (inventory == null)
                throw new NotFoundException($"Inventory với ID {inventoryId} không tồn tại");

            var category = inventory.Categories.FirstOrDefault(c => c.Id == categoryId);
            if (category == null)
                throw new NotFoundException($"Inventory không có category với ID {categoryId}");

            inventory.Categories.Remove(category);
            inventory.UpdatedAt = DateTime.Now;
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync();

            return inventory.ToResponse();
        }

        public async Task<InventoryResponseDto> RemoveTagFromInventoryAsync(int inventoryId, int tagId)
        {
            var inventory = await _unitOfWork.InventoryRepository.GetByIdWithDetailsAsync(inventoryId);
            if (inventory == null)
                throw new NotFoundException($"Inventory với ID {inventoryId} không tồn tại");

            var tag = inventory.Tags.FirstOrDefault(t => t.Id == tagId);
            if (tag == null)
                throw new NotFoundException($"Inventory không có tag với ID {tagId}");

            inventory.Tags.Remove(tag);
            inventory.UpdatedAt = DateTime.Now;
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync();

            return inventory.ToResponse();
        }

        #endregion

        #region Stock Management

        public async Task<InventoryResponseDto> UpdateStockAsync(int id, int quantity)
        {
            var inventory = await _unitOfWork.InventoryRepository.GetByIdWithDetailsAsync(id);
            if (inventory == null)
                throw new NotFoundException($"Inventory với ID {id} không tồn tại");

            if (quantity < 0)
                throw new BadRequestException("Số lượng không thể âm");

            inventory.StockQuantity = quantity;
            inventory.UpdatedAt = DateTime.Now;

            _unitOfWork.InventoryRepository.PrepareUpdate(inventory);
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync();

            return inventory.ToResponse();
        }

        #endregion

        #region Shop Display

        public async Task<PaginatedResult<InventoryListResponseDto>> GetInventoriesForShopAsync(Pagination pagination)
        {
            var cacheKey = $"{SHOP_INVENTORIES_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<InventoryListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.InventoryRepository.GetInventoriesForShopAsync(pagination);
            var result = new PaginatedResult<InventoryListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        #endregion

        #region Cache Management

        private async Task InvalidateCacheAsync()
        {
            await _cacheService.RemoveByPrefixAsync(ALL_INVENTORIES_KEY);
            await _cacheService.RemoveByPrefixAsync(ACTIVE_INVENTORIES_KEY);
            await _cacheService.RemoveByPrefixAsync(SHOP_INVENTORIES_KEY);
        }

        #endregion
    }
}
