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
    public class PlantComboService : IPlantComboService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string ALL_COMBOS_KEY = "combos_all";
        private const string ACTIVE_COMBOS_KEY = "combos_active";
        private const string SHOP_COMBOS_KEY = "combos_shop";

        public PlantComboService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        #region CRUD Operations

        public async Task<PaginatedResult<PlantComboListResponseDto>> GetAllCombosAsync(Pagination pagination)
        {
            var cacheKey = $"{ALL_COMBOS_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<PlantComboListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.PlantComboRepository.GetAllWithDetailsAsync(pagination);
            var result = new PaginatedResult<PlantComboListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<PaginatedResult<PlantComboListResponseDto>> GetActiveCombosAsync(Pagination pagination)
        {
            var cacheKey = $"{ACTIVE_COMBOS_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<PlantComboListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.PlantComboRepository.GetActiveWithDetailsAsync(pagination);
            var result = new PaginatedResult<PlantComboListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<PlantComboResponseDto?> GetComboByIdAsync(int id)
        {
            var combo = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(id);
            if (combo == null)
                return null;

            return combo.ToResponse();
        }

        public async Task<PlantComboResponseDto> CreateComboAsync(PlantComboRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                if (!string.IsNullOrEmpty(request.ComboCode))
                {
                    if (await _unitOfWork.PlantComboRepository.ExistsByCodeAsync(request.ComboCode))
                        throw new BadRequestException($"Combo với mã '{request.ComboCode}' đã tồn tại");
                }

                var combo = request.ToEntity();
                combo.ComboCode ??= PlantComboMapper.GenerateComboCode();

                _unitOfWork.PlantComboRepository.PrepareCreate(combo);
                await _unitOfWork.SaveAsync();

                // Add combo items
                if (request.ComboItems.Any())
                {
                    foreach (var itemDto in request.ComboItems)
                    {
                        // Validate plant exists
                        var plant = await _unitOfWork.PlantRepository.GetByIdAsync(itemDto.PlantId);
                        if (plant == null)
                            throw new NotFoundException($"Plant với ID {itemDto.PlantId} không tồn tại");

                        var comboItem = itemDto.ToComboItemEntity(combo.Id);
                        combo.PlantComboItems.Add(comboItem);
                    }
                    await _unitOfWork.SaveAsync();
                }

                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                // Reload with details
                var created = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(combo.Id);
                return created!.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PlantComboResponseDto> UpdateComboAsync(int id, PlantComboUpdateDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var combo = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(id);
                if (combo == null)
                    throw new NotFoundException($"Combo với ID {id} không tồn tại");

                if (!string.IsNullOrEmpty(request.ComboCode))
                {
                    if (await _unitOfWork.PlantComboRepository.ExistsByCodeAsync(request.ComboCode, id))
                        throw new BadRequestException($"Combo với mã '{request.ComboCode}' đã tồn tại");
                }

                request.ToUpdate(combo);

                _unitOfWork.PlantComboRepository.PrepareUpdate(combo);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return combo.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<bool> DeleteComboAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var combo = await _unitOfWork.PlantComboRepository.GetByIdWithOrdersAsync(id);
                if (combo == null)
                    throw new NotFoundException($"Combo với ID {id} không tồn tại");

                if (combo.CartItems.Any())
                    throw new BadRequestException("Không thể xóa combo đang có trong giỏ hàng.");

                if (combo.OrderItems.Any())
                    throw new BadRequestException("Không thể xóa combo đã có trong đơn hàng. Vui lòng vô hiệu hóa thay vì xóa.");

                combo.IsActive = false;
                combo.UpdatedAt = DateTime.Now;

                _unitOfWork.PlantComboRepository.PrepareUpdate(combo);
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
            var combo = await _unitOfWork.PlantComboRepository.GetByIdAsync(id);
            if (combo == null)
                throw new NotFoundException($"Combo với ID {id} không tồn tại");

            combo.IsActive = !combo.IsActive;
            combo.UpdatedAt = DateTime.Now;

            _unitOfWork.PlantComboRepository.PrepareUpdate(combo);
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync();

            return combo.IsActive ?? true;
        }

        #endregion

        #region Combo Items Management

        public async Task<PlantComboResponseDto> AddComboItemAsync(int comboId, PlantComboItemRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var combo = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
                if (combo == null)
                    throw new NotFoundException($"Combo với ID {comboId} không tồn tại");

                // Validate plant exists
                var plant = await _unitOfWork.PlantRepository.GetByIdAsync(request.PlantId);
                if (plant == null)
                    throw new NotFoundException($"Plant với ID {request.PlantId} không tồn tại");

                // Check if plant already exists in combo
                var existingItem = combo.PlantComboItems.FirstOrDefault(i => i.PlantId == request.PlantId);
                if (existingItem != null)
                    throw new BadRequestException($"Plant với ID {request.PlantId} đã có trong combo");

                var comboItem = request.ToComboItemEntity(comboId);
                combo.PlantComboItems.Add(comboItem);
                combo.UpdatedAt = DateTime.Now;

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                // Reload with details
                var updated = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
                return updated!.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PlantComboResponseDto> RemoveComboItemAsync(int comboId, int comboItemId)
        {
            var combo = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
            if (combo == null)
                throw new NotFoundException($"Combo với ID {comboId} không tồn tại");

            var item = combo.PlantComboItems.FirstOrDefault(i => i.Id == comboItemId);
            if (item == null)
                throw new NotFoundException($"Combo item với ID {comboItemId} không tồn tại trong combo");

            combo.PlantComboItems.Remove(item);
            combo.UpdatedAt = DateTime.Now;
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync();

            // Reload with details
            var updated = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
            return updated!.ToResponse();
        }

        public async Task<PlantComboResponseDto> UpdateComboItemAsync(int comboItemId, PlantComboItemRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Find the combo containing this combo item
                var combo = await _unitOfWork.PlantComboRepository.GetComboByComboItemIdAsync(comboItemId);
                if (combo == null)
                    throw new NotFoundException($"Combo item với ID {comboItemId} không tồn tại");

                var comboItem = combo.PlantComboItems.First(i => i.Id == comboItemId);

                // Validate plant exists
                var plant = await _unitOfWork.PlantRepository.GetByIdAsync(request.PlantId);
                if (plant == null)
                    throw new NotFoundException($"Plant với ID {request.PlantId} không tồn tại");

                comboItem.PlantId = request.PlantId;
                comboItem.Quantity = request.Quantity;
                comboItem.Notes = request.Notes;
                combo.UpdatedAt = DateTime.Now;

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                // Reload with details
                var updated = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(combo.Id);
                return updated!.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        #endregion

        #region Tag Assignment

        public async Task<PlantComboResponseDto> AssignTagsToComboAsync(AssignComboTagsDto request)
        {
            var combo = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(request.PlantComboId);
            if (combo == null)
                throw new NotFoundException($"Combo với ID {request.PlantComboId} không tồn tại");

            var tags = await _unitOfWork.TagRepository.GetByIdsAsync(request.TagIds);

            if (tags.Count != request.TagIds.Count)
            {
                var invalidIds = request.TagIds.Except(tags.Select(t => t.Id));
                throw new NotFoundException($"Các Tag với ID {string.Join(", ", invalidIds)} không tồn tại");
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                combo.TagsNavigation.Clear();
                foreach (var tag in tags)
                {
                    combo.TagsNavigation.Add(tag);
                }

                combo.UpdatedAt = DateTime.Now;
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return combo.ToResponse();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PlantComboResponseDto> RemoveTagFromComboAsync(int comboId, int tagId)
        {
            var combo = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
            if (combo == null)
                throw new NotFoundException($"Combo với ID {comboId} không tồn tại");

            var tag = combo.TagsNavigation.FirstOrDefault(t => t.Id == tagId);
            if (tag == null)
                throw new NotFoundException($"Combo không có tag với ID {tagId}");

            combo.TagsNavigation.Remove(tag);
            combo.UpdatedAt = DateTime.Now;
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync();

            return combo.ToResponse();
        }

        #endregion

        #region Shop Display

        public async Task<PaginatedResult<PlantComboListResponseDto>> GetCombosForShopAsync(Pagination pagination)
        {
            var cacheKey = $"{SHOP_COMBOS_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<PlantComboListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.PlantComboRepository.GetCombosForShopAsync(pagination);
            var result = new PaginatedResult<PlantComboListResponseDto>(
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
            await _cacheService.RemoveByPrefixAsync(ALL_COMBOS_KEY);
            await _cacheService.RemoveByPrefixAsync(ACTIVE_COMBOS_KEY);
            await _cacheService.RemoveByPrefixAsync(SHOP_COMBOS_KEY);
        }

        #endregion
    }
}
