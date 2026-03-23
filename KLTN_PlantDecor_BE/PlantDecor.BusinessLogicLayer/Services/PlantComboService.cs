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
        private const string NURSERIES_BY_COMBO_KEY = "nurseries_by_combo";

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

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(10));
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

                // Add combo items and determine safety
                var plantsInCombo = new List<Plant>();
                if (request.ComboItems.Any())
                {
                    foreach (var itemDto in request.ComboItems)
                    {
                        var plant = await _unitOfWork.PlantRepository.GetByIdAsync(itemDto.PlantId)
                            ?? throw new NotFoundException($"Plant với ID {itemDto.PlantId} không tồn tại");
                        plantsInCombo.Add(plant);
                    }
                }

                // Recalculate safety based on plants
                combo.PetSafe = !plantsInCombo.Any() || plantsInCombo.All(p => p.PetSafe == true);
                combo.ChildSafe = !plantsInCombo.Any() || plantsInCombo.All(p => p.ChildSafe == true);

                _unitOfWork.PlantComboRepository.PrepareCreate(combo);
                await _unitOfWork.SaveAsync();

                if (plantsInCombo.Any())
                {
                    foreach (var itemDto in request.ComboItems)
                    {
                        var comboItem = itemDto.ToComboItemEntity(combo.Id);
                        combo.PlantComboItems.Add(comboItem);
                    }
                    await _unitOfWork.SaveAsync();
                }

                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(combo.Id);

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

                // Recalculate safety based on existing plants in the combo
                var plantsInCombo = combo.PlantComboItems.Select(ci => ci.Plant).ToList();
                combo.PetSafe = !plantsInCombo.Any() || plantsInCombo.All(p => p?.PetSafe == true);
                combo.ChildSafe = !plantsInCombo.Any() || plantsInCombo.All(p => p?.ChildSafe == true);

                _unitOfWork.PlantComboRepository.PrepareUpdate(combo);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(id);

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

                // Check if combo is in use via NurseryPlantCombos
                if (combo.NurseryPlantCombos.Any())
                    throw new BadRequestException("Không thể xóa combo đang được liên kết với nursery. Vui lòng vô hiệu hóa thay vì xóa.");

                combo.IsActive = false;
                combo.UpdatedAt = DateTime.Now;

                _unitOfWork.PlantComboRepository.PrepareUpdate(combo);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(id);

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

            await InvalidateCacheAsync(id);

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
                var plant = await _unitOfWork.PlantRepository.GetByIdAsync(request.PlantId)
                    ?? throw new NotFoundException($"Plant với ID {request.PlantId} không tồn tại");

                // Check if plant already exists in combo
                if (combo.PlantComboItems.Any(i => i.PlantId == request.PlantId))
                    throw new BadRequestException($"Plant với ID {request.PlantId} đã có trong combo");

                var comboItem = request.ToComboItemEntity(comboId);
                combo.PlantComboItems.Add(comboItem);
                combo.UpdatedAt = DateTime.Now;

                // Recalculate safety
                var plantsInCombo = combo.PlantComboItems.Select(ci => ci.Plant).ToList();
                plantsInCombo.Add(plant); // Add the new plant for calculation
                combo.PetSafe = plantsInCombo.All(p => p?.PetSafe == true);
                combo.ChildSafe = plantsInCombo.All(p => p?.ChildSafe == true);

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(comboId);

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

            // Recalculate safety
            var plantsInCombo = combo.PlantComboItems.Select(ci => ci.Plant).ToList();
            combo.PetSafe = !plantsInCombo.Any() || plantsInCombo.All(p => p?.PetSafe == true);
            combo.ChildSafe = !plantsInCombo.Any() || plantsInCombo.All(p => p?.ChildSafe == true);

            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync(comboId);

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
                var plant = await _unitOfWork.PlantRepository.GetByIdAsync(request.PlantId)
                    ?? throw new NotFoundException($"Plant với ID {request.PlantId} không tồn tại");

                comboItem.PlantId = request.PlantId;
                comboItem.Quantity = request.Quantity;
                comboItem.Notes = request.Notes;
                combo.UpdatedAt = DateTime.Now;

                // Recalculate safety
                // Manually update the plant in the collection for accurate calculation
                comboItem.Plant = plant;
                var plantsInCombo = combo.PlantComboItems.Select(ci => ci.Plant).ToList();
                combo.PetSafe = !plantsInCombo.Any() || plantsInCombo.All(p => p?.PetSafe == true);
                combo.ChildSafe = !plantsInCombo.Any() || plantsInCombo.All(p => p?.ChildSafe == true);

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(combo.Id);

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

                await InvalidateCacheAsync(request.PlantComboId);

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

            await InvalidateCacheAsync(comboId);

            return combo.ToResponse();
        }

        #endregion

        #region Manager - Nursery Combo Stock

        public async Task<NurseryComboStockOperationResponseDto> AssembleComboStockAsync(int nurseryId, int managerId, int comboId, AssembleNurseryComboRequestDto request)
        {
            if (request.Quantity <= 0)
                throw new BadRequestException("Số lượng combo tạo phải lớn hơn 0");

            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null || nursery.Id != nurseryId)
                throw new ForbiddenException("Bạn không có quyền thao tác với vựa này");

            var combo = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
            if (combo == null)
                throw new NotFoundException($"Combo với ID {comboId} không tồn tại");

            if (!combo.PlantComboItems.Any())
                throw new BadRequestException("Combo chưa có thành phần cây để tạo tồn kho kinh doanh");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var stockChanges = new List<NurseryComboPlantStockChangeDto>();
                var shortageMessages = new List<string>();

                foreach (var item in combo.PlantComboItems)
                {
                    if (!item.PlantId.HasValue)
                        throw new BadRequestException($"Combo item ID {item.Id} chưa có PlantId hợp lệ");

                    if (!item.Quantity.HasValue || item.Quantity.Value <= 0)
                        throw new BadRequestException($"Combo item ID {item.Id} có số lượng cây không hợp lệ");

                    var requiredQuantity = item.Quantity.Value * request.Quantity;
                    var commonPlant = await _unitOfWork.CommonPlantRepository.GetByPlantAndNurseryAsync(item.PlantId.Value, nurseryId);
                    if (commonPlant == null || !commonPlant.IsActive)
                    {
                        shortageMessages.Add($"- PlantId {item.PlantId.Value}: không tồn tại trong kho cây đại trà của vựa hoặc đang bị vô hiệu hóa");
                        continue;
                    }

                    var availableStock = commonPlant.Quantity - commonPlant.ReservedQuantity;
                    if (availableStock < requiredQuantity)
                    {
                        var plantName = item.Plant?.Name ?? $"PlantId {item.PlantId.Value}";
                        shortageMessages.Add($"- {plantName}: cần {requiredQuantity}, khả dụng {availableStock}");
                        continue;
                    }

                    var beforeStock = commonPlant.Quantity;
                    commonPlant.Quantity -= requiredQuantity;
                    _unitOfWork.CommonPlantRepository.PrepareUpdate(commonPlant);

                    stockChanges.Add(new NurseryComboPlantStockChangeDto
                    {
                        PlantId = item.PlantId.Value,
                        PlantName = item.Plant?.Name ?? string.Empty,
                        QuantityPerCombo = item.Quantity.Value,
                        QuantityChanged = requiredQuantity,
                        StockBefore = beforeStock,
                        StockAfter = commonPlant.Quantity
                    });
                }

                if (shortageMessages.Any())
                {
                    throw new BadRequestException("Không đủ tồn kho cây đại trà để tạo combo:\n" + string.Join("\n", shortageMessages));
                }

                var nurseryCombo = await _unitOfWork.NurseryPlantComboRepository.GetByNurseryAndComboAsync(nurseryId, comboId);
                var comboStockBefore = nurseryCombo?.Quantity ?? 0;

                if (nurseryCombo == null)
                {
                    nurseryCombo = new NurseryPlantCombo
                    {
                        NurseryId = nurseryId,
                        PlantComboId = comboId,
                        Quantity = request.Quantity,
                        IsActive = true,
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now
                    };
                    _unitOfWork.NurseryPlantComboRepository.PrepareCreate(nurseryCombo);
                }
                else
                {
                    nurseryCombo.Quantity += request.Quantity;
                    nurseryCombo.IsActive = true;
                    nurseryCombo.UpdatedAt = DateTime.Now;
                    _unitOfWork.NurseryPlantComboRepository.PrepareUpdate(nurseryCombo);
                }

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(comboId);

                return new NurseryComboStockOperationResponseDto
                {
                    NurseryId = nurseryId,
                    PlantComboId = comboId,
                    ComboName = combo.ComboName,
                    OperationType = "assemble",
                    QuantityProcessed = request.Quantity,
                    ComboStockBefore = comboStockBefore,
                    ComboStockAfter = nurseryCombo.Quantity,
                    PlantStockChanges = stockChanges
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<NurseryComboStockOperationResponseDto> DecomposeComboStockAsync(int nurseryId, int managerId, int comboId, DecomposeNurseryComboRequestDto request)
        {
            if (request.Quantity <= 0)
                throw new BadRequestException("Số lượng combo phân rã phải lớn hơn 0");

            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null || nursery.Id != nurseryId)
                throw new ForbiddenException("Bạn không có quyền thao tác với vựa này");

            var combo = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
            if (combo == null)
                throw new NotFoundException($"Combo với ID {comboId} không tồn tại");

            if (!combo.PlantComboItems.Any())
                throw new BadRequestException("Combo chưa có thành phần cây để phân rã");

            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var nurseryCombo = await _unitOfWork.NurseryPlantComboRepository.GetByNurseryAndComboAsync(nurseryId, comboId);
                if (nurseryCombo == null || !nurseryCombo.IsActive)
                    throw new NotFoundException("Vựa chưa có tồn kho combo này để phân rã");

                var comboStockBefore = nurseryCombo.Quantity;
                if (comboStockBefore < request.Quantity)
                    throw new BadRequestException($"Không đủ số lượng combo để phân rã. Hiện có {comboStockBefore}, yêu cầu {request.Quantity}");

                var stockChanges = new List<NurseryComboPlantStockChangeDto>();
                foreach (var item in combo.PlantComboItems)
                {
                    if (!item.PlantId.HasValue)
                        throw new BadRequestException($"Combo item ID {item.Id} chưa có PlantId hợp lệ");

                    if (!item.Quantity.HasValue || item.Quantity.Value <= 0)
                        throw new BadRequestException($"Combo item ID {item.Id} có số lượng cây không hợp lệ");

                    var returnQuantity = item.Quantity.Value * request.Quantity;
                    var commonPlant = await _unitOfWork.CommonPlantRepository.GetByPlantAndNurseryAsync(item.PlantId.Value, nurseryId);
                    var isNewCommonPlant = false;

                    if (commonPlant == null)
                    {
                        commonPlant = new CommonPlant
                        {
                            PlantId = item.PlantId.Value,
                            NurseryId = nurseryId,
                            Quantity = 0,
                            ReservedQuantity = 0,
                            IsActive = true
                        };
                        isNewCommonPlant = true;
                    }

                    var beforeStock = commonPlant.Quantity;
                    commonPlant.Quantity += returnQuantity;
                    commonPlant.IsActive = true;
                    if (isNewCommonPlant)
                    {
                        _unitOfWork.CommonPlantRepository.PrepareCreate(commonPlant);
                    }
                    else
                    {
                        _unitOfWork.CommonPlantRepository.PrepareUpdate(commonPlant);
                    }

                    stockChanges.Add(new NurseryComboPlantStockChangeDto
                    {
                        PlantId = item.PlantId.Value,
                        PlantName = item.Plant?.Name ?? string.Empty,
                        QuantityPerCombo = item.Quantity.Value,
                        QuantityChanged = returnQuantity,
                        StockBefore = beforeStock,
                        StockAfter = commonPlant.Quantity
                    });
                }

                nurseryCombo.Quantity -= request.Quantity;
                nurseryCombo.IsActive = nurseryCombo.Quantity > 0;
                nurseryCombo.UpdatedAt = DateTime.Now;
                _unitOfWork.NurseryPlantComboRepository.PrepareUpdate(nurseryCombo);

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(comboId);

                return new NurseryComboStockOperationResponseDto
                {
                    NurseryId = nurseryId,
                    PlantComboId = comboId,
                    ComboName = combo.ComboName,
                    OperationType = "decompose",
                    QuantityProcessed = request.Quantity,
                    ComboStockBefore = comboStockBefore,
                    ComboStockAfter = nurseryCombo.Quantity,
                    PlantStockChanges = stockChanges
                };
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
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

        public async Task<PaginatedResult<SellingPlantComboResponseDto>> GetSellingCombosAsync(Pagination pagination, PlantComboShopSearchRequestDto searchDto)
        {
            IQueryable<NurseryPlantCombo> query = _unitOfWork.NurseryPlantComboRepository.GetQuery()
                .Where(npc => npc.Quantity > 0 && npc.PlantCombo.IsActive == true);

            query = query
                .Include(npc => npc.PlantCombo)
                    .ThenInclude(pc => pc.PlantComboImages)
                .Include(npc => npc.PlantCombo)
                    .ThenInclude(pc => pc.TagsNavigation)
                .Include(npc => npc.Nursery);

            // Keyword search
            if (!string.IsNullOrWhiteSpace(searchDto.Keyword))
            {
                var keyword = searchDto.Keyword.ToLower();
                query = query.Where(npc => (npc.PlantCombo.ComboName != null && npc.PlantCombo.ComboName.ToLower().Contains(keyword)) ||
                                           (npc.PlantCombo.Description != null && npc.PlantCombo.Description.ToLower().Contains(keyword)));
            }

            // Price range
            if (searchDto.MinPrice.HasValue)
            {
                query = query.Where(npc => npc.PlantCombo.ComboPrice.HasValue && npc.PlantCombo.ComboPrice.Value >= searchDto.MinPrice.Value);
            }
            if (searchDto.MaxPrice.HasValue)
            {
                query = query.Where(npc => npc.PlantCombo.ComboPrice.HasValue && npc.PlantCombo.ComboPrice.Value <= searchDto.MaxPrice.Value);
            }

            // Safety filters
            if (searchDto.PetSafe.HasValue)
            {
                query = query.Where(npc => npc.PlantCombo.PetSafe == searchDto.PetSafe.Value);
            }
            if (searchDto.ChildSafe.HasValue)
            {
                query = query.Where(npc => npc.PlantCombo.ChildSafe == searchDto.ChildSafe.Value);
            }

            // Category filter: combo is matched when any plant in the combo belongs to CategoryId
            if (searchDto.CategoryId.HasValue)
            {
                query = query.Where(npc => npc.PlantCombo.PlantComboItems
                    .Any(ci => ci.Plant != null && ci.Plant.Categories.Any(c => c.Id == searchDto.CategoryId.Value)));
            }

            // Tags
            if (searchDto.TagIds != null && searchDto.TagIds.Any())
            {
                query = query.Where(npc => npc.PlantCombo.TagsNavigation.Any(tag => searchDto.TagIds.Contains(tag.Id)));
            }

            var sellingComboEntries = await query.ToListAsync();

            var groupedByCombo = sellingComboEntries
                .GroupBy(npc => npc.PlantCombo)
                .Select(g => new SellingPlantComboResponseDto
                {
                    Id = g.Key.Id,
                    Name = g.Key.ComboName ?? string.Empty,
                    Description = g.Key.Description,
                    Price = g.Key.ComboPrice ?? 0,
                    ImageUrl = g.Key.PlantComboImages.FirstOrDefault()?.ImageUrl,
                    AverageRating = 0,
                    Nurseries = g.Select(npc => new SellingNurseryResponseDto
                    {
                        NurseryId = npc.NurseryId,
                        NurseryName = npc.Nursery.Name ?? string.Empty,
                        Quantity = npc.Quantity
                    }).ToList()
                });

            var totalCount = groupedByCombo.Count();
            var paginatedItems = groupedByCombo.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();

            return new PaginatedResult<SellingPlantComboResponseDto>(paginatedItems, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        #endregion

        #region Cache Management

        private async Task InvalidateCacheAsync()
        {
            await _cacheService.RemoveByPrefixAsync(ALL_COMBOS_KEY);
            await _cacheService.RemoveByPrefixAsync(ACTIVE_COMBOS_KEY);
            await _cacheService.RemoveByPrefixAsync(SHOP_COMBOS_KEY);
        }

        private async Task InvalidateCacheAsync(int? comboId = null)
        {
            await _cacheService.RemoveByPrefixAsync(ALL_COMBOS_KEY);
            await _cacheService.RemoveByPrefixAsync(ACTIVE_COMBOS_KEY);
            await _cacheService.RemoveByPrefixAsync(SHOP_COMBOS_KEY);
            await _cacheService.RemoveByPrefixAsync("selling_combos_");
            if (comboId.HasValue)
            {
                await _cacheService.RemoveByPrefixAsync($"{NURSERIES_BY_COMBO_KEY}_{comboId.Value}");
            }
        }

        #endregion

        #region Private Methods

        public async Task<List<NurseryListResponseDto>> GetNurseriesByComboAsync(int comboId)
        {
            var cacheKey = $"{NURSERIES_BY_COMBO_KEY}_{comboId}";
            var cachedData = await _cacheService.GetDataAsync<List<NurseryListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var combo = await _unitOfWork.PlantComboRepository.GetByIdAsync(comboId);
            if (combo == null)
            {
                throw new NotFoundException($"Plant combo với ID '{comboId}' không tồn tại");
            }

            var nurseryPlantCombos = await _unitOfWork.NurseryPlantComboRepository.GetQuery()
                .Where(npc => npc.PlantComboId == comboId && npc.Quantity > 0 && npc.IsActive)
                .Include(npc => npc.Nursery)
                .ToListAsync();

            var nurseries = nurseryPlantCombos.Select(npc => npc.Nursery).Where(n => n != null && n.IsActive == true).ToList();

            var result = nurseries.Select(n => n!.ToListResponse()).ToList();
            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(15));
            return result;
        }

        #endregion
    }
}
