using Hangfire;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PlantDecor.BusinessLogicLayer.Constants;
using PlantDecor.BusinessLogicLayer.DTOs.Embedding;
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
    public class PlantComboService : IPlantComboService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        private const string ALL_COMBOS_KEY = "combos_all";
        private const string ACTIVE_COMBOS_KEY = "combos_active";
        private const string SHOP_COMBOS_KEY = "combos_shop";
        private const string NURSERIES_BY_COMBO_KEY = "nurseries_by_combo";
        private const string ALL_COMMON_PLANTS_KEY = "common_plants_all";
        private const string NURSERY_COMMON_PLANTS_KEY = "nursery_common_plants";
        private const string PLANT_NURSERIES_COMMON_KEY = "plant_nurseries_common";
        private const string PLANT_SHOP_SEARCH_KEY = "plants_shop_search";

        public PlantComboService(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ICloudinaryService cloudinaryService,
            IBackgroundJobClient backgroundJobClient)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _cloudinaryService = cloudinaryService;
            _backgroundJobClient = backgroundJobClient;
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

        public async Task<PlantComboResponseDto> GetComboByIdAsync(int id)
        {
            var combo = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(id);
            if (combo == null)
                throw new NotFoundException($"Combo với ID {id} không tồn tại");

            return combo.ToResponse();
        }

        public async Task<PlantComboResponseDto> CreateComboAsync(PlantComboRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var normalizedComboCode = string.IsNullOrWhiteSpace(request.ComboCode)
                    ? null
                    : request.ComboCode.Trim().ToUpper();
                var normalizedComboName = request.ComboName?.Trim();

                if (!string.IsNullOrEmpty(normalizedComboCode))
                {
                    if (await _unitOfWork.PlantComboRepository.ExistsByCodeAsync(normalizedComboCode))
                        throw new BadRequestException($"Combo với mã '{normalizedComboCode}' đã tồn tại");
                }

                if (!string.IsNullOrWhiteSpace(normalizedComboName))
                {
                    if (await _unitOfWork.PlantComboRepository.ExistsByNameAsync(normalizedComboName))
                        throw new BadRequestException($"Combo với tên '{normalizedComboName}' đã tồn tại");
                }

                ValidateEnumBackedFields(request.SuitableSpace, request.SuitableRooms);

                request.ComboName = normalizedComboName ?? request.ComboName;
                var combo = request.ToEntity();
                combo.ComboCode = normalizedComboCode ?? PlantComboMapper.GenerateComboCode();

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

                var tagIds = (request.TagIds ?? new List<int>()).Distinct().ToList();
                if (tagIds.Any())
                {
                    var tags = await _unitOfWork.TagRepository.GetByIdsAsync(tagIds);
                    if (tags.Count != tagIds.Count)
                    {
                        var invalidIds = tagIds.Except(tags.Select(t => t.Id));
                        throw new NotFoundException($"Các Tag với ID {string.Join(", ", invalidIds)} không tồn tại");
                    }

                    foreach (var tag in tags)
                    {
                        combo.TagsNavigation.Add(tag);
                    }
                }

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

                var normalizedComboCode = string.IsNullOrWhiteSpace(request.ComboCode)
                    ? null
                    : request.ComboCode.Trim().ToUpper();

                if (!string.IsNullOrEmpty(normalizedComboCode))
                {
                    if (await _unitOfWork.PlantComboRepository.ExistsByCodeAsync(normalizedComboCode, id))
                        throw new BadRequestException($"Combo với mã '{normalizedComboCode}' đã tồn tại");
                }

                ValidateEnumBackedFields(request.SuitableSpace, request.SuitableRooms);

                request.ComboCode = normalizedComboCode;

                request.ToUpdate(combo);

                if (request.TagIds != null)
                {
                    var tagIds = request.TagIds.Distinct().ToList();
                    var tags = await _unitOfWork.TagRepository.GetByIdsAsync(tagIds);

                    if (tags.Count != tagIds.Count)
                    {
                        var invalidIds = tagIds.Except(tags.Select(t => t.Id));
                        throw new NotFoundException($"Các Tag với ID {string.Join(", ", invalidIds)} không tồn tại");
                    }

                    combo.TagsNavigation.Clear();
                    foreach (var tag in tags)
                    {
                        combo.TagsNavigation.Add(tag);
                    }
                }

                // Recalculate safety based on existing plants in the combo
                var plantsInCombo = combo.PlantComboItems.Select(ci => ci.Plant).ToList();
                combo.PetSafe = !plantsInCombo.Any() || plantsInCombo.All(p => p?.PetSafe == true);
                combo.ChildSafe = !plantsInCombo.Any() || plantsInCombo.All(p => p?.ChildSafe == true);

                _unitOfWork.PlantComboRepository.PrepareUpdate(combo);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync(id);
                await QueueNurseryPlantComboEmbeddingsByComboIdAsync(id);

                return combo.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PlantComboResponseDto> UploadPlantComboImagesAsync(int comboId, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                throw new BadRequestException("No files were uploaded");

            var combo = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
            if (combo == null)
                throw new NotFoundException($"Combo với ID {comboId} không tồn tại");

            foreach (var file in files)
            {
                var (isValid, errorMessage) = _cloudinaryService.ValidateDocumentFile(file);
                if (!isValid)
                    throw new BadRequestException(errorMessage);
            }

            List<FileUploadResponse> uploadedFiles = new();

            try
            {
                uploadedFiles = await _cloudinaryService.UploadFilesAsync(files, "PlantComboImages");
                if (uploadedFiles.Count == 0)
                    throw new BadRequestException("Plant combo images upload failed");

                await _unitOfWork.BeginTransactionAsync();

                foreach (var uploadedFile in uploadedFiles)
                {
                    combo.PlantComboImages.Add(new PlantComboImage
                    {
                        PlantComboId = combo.Id,
                        ImageUrl = uploadedFile.SecureUrl,
                        IsPrimary = false,
                        CreatedAt = DateTime.Now
                    });
                }

                combo.UpdatedAt = DateTime.Now;
                _unitOfWork.PlantComboRepository.PrepareUpdate(combo);

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();

                foreach (var uploadedFile in uploadedFiles)
                {
                    if (string.IsNullOrWhiteSpace(uploadedFile.PublicId))
                        continue;

                    try
                    {
                        await _cloudinaryService.DeleteFileAsync(uploadedFile.PublicId);
                    }
                    catch
                    {
                    }
                }

                throw;
            }

            await InvalidateCacheAsync(comboId);

            var updatedCombo = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
            return updatedCombo!.ToResponse();
        }

        public async Task<PlantComboResponseDto> UploadPlantComboThumbnailAsync(int comboId, IFormFile file)
        {
            if (file == null)
                throw new BadRequestException("No file was uploaded");

            var combo = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
            if (combo == null)
                throw new NotFoundException($"Combo với ID {comboId} không tồn tại");

            var (isValid, errorMessage) = _cloudinaryService.ValidateDocumentFile(file);
            if (!isValid)
                throw new BadRequestException(errorMessage);

            var uploadedFile = await _cloudinaryService.UploadFileAsync(file, "PlantComboImages");
            if (uploadedFile == null || string.IsNullOrWhiteSpace(uploadedFile.SecureUrl))
                throw new BadRequestException("Plant combo thumbnail upload failed");

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                foreach (var image in combo.PlantComboImages)
                {
                    image.IsPrimary = false;
                }

                combo.PlantComboImages.Add(new PlantComboImage
                {
                    PlantComboId = combo.Id,
                    ImageUrl = uploadedFile.SecureUrl,
                    IsPrimary = true,
                    CreatedAt = DateTime.Now
                });

                combo.UpdatedAt = DateTime.Now;
                _unitOfWork.PlantComboRepository.PrepareUpdate(combo);

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();

                if (!string.IsNullOrWhiteSpace(uploadedFile.PublicId))
                {
                    try
                    {
                        await _cloudinaryService.DeleteFileAsync(uploadedFile.PublicId);
                    }
                    catch
                    {
                    }
                }

                throw;
            }

            await InvalidateCacheAsync(comboId);

            var updatedCombo = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
            return updatedCombo!.ToResponse();
        }

        public async Task<PlantComboResponseDto> SetPrimaryPlantComboImageAsync(int comboId, int imageId)
        {
            var combo = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
            if (combo == null)
                throw new NotFoundException($"Combo với ID {comboId} không tồn tại");

            var targetImage = combo.PlantComboImages.FirstOrDefault(i => i.Id == imageId);
            if (targetImage == null)
                throw new NotFoundException($"Ảnh với ID {imageId} không thuộc combo {comboId}");

            foreach (var image in combo.PlantComboImages)
                image.IsPrimary = image.Id == imageId;

            combo.UpdatedAt = DateTime.Now;
            _unitOfWork.PlantComboRepository.PrepareUpdate(combo);
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync(comboId);

            var updated = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
            return updated!.ToResponse();
        }

        public async Task<PlantComboResponseDto> ReplaceImageAsync(int comboId, int imageId, IFormFile file)
        {
            if (file == null)
                throw new BadRequestException("No file was uploaded");

            var combo = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
            if (combo == null)
                throw new NotFoundException($"Combo với ID {comboId} không tồn tại");

            var image = combo.PlantComboImages.FirstOrDefault(i => i.Id == imageId);
            if (image == null)
                throw new NotFoundException($"Ảnh với ID {imageId} không thuộc combo {comboId}");

            var (isValid, errorMessage) = _cloudinaryService.ValidateDocumentFile(file);
            if (!isValid)
                throw new BadRequestException(errorMessage);

            var oldPublicId = ExtractCloudinaryPublicId(image.ImageUrl);

            var uploadedFile = await _cloudinaryService.UploadFileAsync(file, "PlantComboImages");
            if (uploadedFile == null || string.IsNullOrWhiteSpace(uploadedFile.SecureUrl))
                throw new BadRequestException("Image upload failed");

            image.ImageUrl = uploadedFile.SecureUrl;
            combo.UpdatedAt = DateTime.Now;
            _unitOfWork.PlantComboRepository.PrepareUpdate(combo);
            await _unitOfWork.SaveAsync();

            if (!string.IsNullOrWhiteSpace(oldPublicId))
            {
                try { await _cloudinaryService.DeleteFileAsync(oldPublicId); }
                catch { }
            }

            await InvalidateCacheAsync(comboId);

            var updated = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
            return updated!.ToResponse();
        }

        public async Task<PlantComboResponseDto> DeletePlantComboImageAsync(int comboId, int imageId)
        {
            var combo = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
            if (combo == null)
                throw new NotFoundException($"Combo với ID {comboId} không tồn tại");

            var image = combo.PlantComboImages.FirstOrDefault(i => i.Id == imageId);
            if (image == null)
                throw new NotFoundException($"Ảnh với ID {imageId} không thuộc combo {comboId}");

            var wasPrimary = image.IsPrimary == true;
            var publicId = ExtractCloudinaryPublicId(image.ImageUrl);

            combo.PlantComboImages.Remove(image);

            if (wasPrimary)
            {
                var next = combo.PlantComboImages.FirstOrDefault();
                if (next != null) next.IsPrimary = true;
            }

            combo.UpdatedAt = DateTime.Now;
            _unitOfWork.PlantComboRepository.PrepareUpdate(combo);
            await _unitOfWork.SaveAsync();

            if (!string.IsNullOrWhiteSpace(publicId))
            {
                try { await _cloudinaryService.DeleteFileAsync(publicId); }
                catch { }
            }

            await InvalidateCacheAsync(comboId);

            var updated = await _unitOfWork.PlantComboRepository.GetByIdWithDetailsAsync(comboId);
            return updated!.ToResponse();
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
                await QueueNurseryPlantComboEmbeddingsByComboIdAsync(id);

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
            await QueueNurseryPlantComboEmbeddingsByComboIdAsync(id);

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
                await QueueNurseryPlantComboEmbeddingsByComboIdAsync(comboId);

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
            await QueueNurseryPlantComboEmbeddingsByComboIdAsync(comboId);

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
                await QueueNurseryPlantComboEmbeddingsByComboIdAsync(combo.Id);

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
                await QueueNurseryPlantComboEmbeddingsByComboIdAsync(request.PlantComboId);

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
            await QueueNurseryPlantComboEmbeddingsByComboIdAsync(comboId);

            return combo.ToResponse();
        }

        #endregion

        #region Manager - Nursery Combo Stock

        public async Task<NurseryComboStockOperationResponseDto> AssembleComboStockAsync(int managerId, int comboId, AssembleNurseryComboRequestDto request)
        {
            if (request.Quantity <= 0)
                throw new BadRequestException("Số lượng combo tạo phải lớn hơn 0");

            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("Bạn không có quyền thao tác với vựa này");

            var nurseryId = nursery.Id;

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

                    var availableStock = commonPlant.Quantity;
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

                await InvalidateCacheAsync(comboId, nurseryId);

                // Queue embedding for NurseryPlantCombo
                var reloadedNurseryCombo = await _unitOfWork.NurseryPlantComboRepository.GetByIdAsync(nurseryCombo.Id);
                if (reloadedNurseryCombo != null)
                {
                    QueueNurseryPlantComboEmbeddingAsync(reloadedNurseryCombo, combo);
                }

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

        public async Task<NurseryComboStockOperationResponseDto> DecomposeComboStockAsync(int managerId, int comboId, DecomposeNurseryComboRequestDto request)
        {
            if (request.Quantity <= 0)
                throw new BadRequestException("Số lượng combo phân rã phải lớn hơn 0");

            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("Bạn không có quyền thao tác với vựa này");

            var nurseryId = nursery.Id;

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
                var affectedCommonPlants = new List<CommonPlant>();
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

                    affectedCommonPlants.Add(commonPlant);

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

                await InvalidateCacheAsync(comboId, nurseryId);
                await QueueNurseryPlantComboEmbeddingsByComboIdAsync(comboId);
                foreach (var commonPlantId in affectedCommonPlants.Select(cp => cp.Id).Where(id => id > 0).Distinct())
                {
                    await QueueCommonPlantEmbeddingByIdAsync(commonPlantId);
                }

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

        public async Task<PaginatedResult<NurseryComboStockResponseDto>> GetNurseryComboStockAsync(int managerId, Pagination pagination)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("Bạn không có quyền truy cập tài nguyên này");

            var query = _unitOfWork.NurseryPlantComboRepository.GetQuery()
                .Where(npc => npc.NurseryId == nursery.Id)
                .Include(npc => npc.PlantCombo)
                    .ThenInclude(pc => pc.PlantComboImages);

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(npc => npc.UpdatedAt ?? npc.CreatedAt)
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();

            var dtos = items.Select(npc => new NurseryComboStockResponseDto
            {
                Id = npc.Id,
                PlantComboId = npc.PlantComboId,
                ComboCode = npc.PlantCombo.ComboCode,
                ComboName = npc.PlantCombo.ComboName,
                ComboType = npc.PlantCombo.ComboType,
                ComboTypeName = MapComboTypeName(npc.PlantCombo.ComboType),
                Price = npc.PlantCombo.ComboPrice,
                Quantity = npc.Quantity,
                IsActive = npc.IsActive,
                PrimaryImageUrl = npc.PlantCombo.PlantComboImages
                    .FirstOrDefault(img => img.IsPrimary == true)?.ImageUrl
                    ?? npc.PlantCombo.PlantComboImages.FirstOrDefault()?.ImageUrl,
                CreatedAt = npc.CreatedAt,
                UpdatedAt = npc.UpdatedAt
            }).ToList();

            return new PaginatedResult<NurseryComboStockResponseDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize);
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

            if (searchDto.NurseryId.HasValue)
            {
                query = query.Where(npc => npc.NurseryId == searchDto.NurseryId.Value);
            }

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
                // Name-only search: keyword chỉ áp dụng trên tên combo
                query = query.Where(npc =>
                    npc.PlantCombo.ComboName != null && npc.PlantCombo.ComboName.ToLower().Contains(keyword));
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

            // Season filter
            if (searchDto.Season.HasValue)
            {
                query = query.Where(npc => npc.PlantCombo.Season.HasValue && npc.PlantCombo.Season.Value == searchDto.Season.Value);
            }

            // Combo type filter
            if (searchDto.ComboType.HasValue)
            {
                query = query.Where(npc => npc.PlantCombo.ComboType.HasValue && npc.PlantCombo.ComboType.Value == searchDto.ComboType.Value);
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
                    ComboType = g.Key.ComboType,
                    ComboTypeName = MapComboTypeName(g.Key.ComboType),
                    Description = g.Key.Description,
                    Price = g.Key.ComboPrice ?? 0,
                    PrimaryImageUrl = g.Key.PlantComboImages.FirstOrDefault(i => i.IsPrimary == true)?.ImageUrl
                               ?? g.Key.PlantComboImages.FirstOrDefault()?.ImageUrl,
                    Nurseries = g.Select(npc => new SellingNurseryResponseDto
                    {
                        NurseryId = npc.NurseryId,
                        NurseryName = npc.Nursery.Name ?? string.Empty,
                        Quantity = npc.Quantity
                    }).ToList()
                });

            groupedByCombo = ApplySellingComboSort(groupedByCombo, searchDto);

            var totalCount = groupedByCombo.Count();
            var paginatedItems = groupedByCombo.Skip((pagination.PageNumber - 1) * pagination.PageSize).Take(pagination.PageSize).ToList();

            return new PaginatedResult<SellingPlantComboResponseDto>(paginatedItems, totalCount, pagination.PageNumber, pagination.PageSize);
        }

        private static IEnumerable<SellingPlantComboResponseDto> ApplySellingComboSort(
            IEnumerable<SellingPlantComboResponseDto> source,
            PlantComboShopSearchRequestDto searchDto)
        {
            var sortBy = searchDto.SortBy ?? PlantComboSortByEnum.Name;
            var isDescending = (searchDto.SortDirection ?? SortDirectionEnum.Asc) == SortDirectionEnum.Desc;

            return sortBy switch
            {
                PlantComboSortByEnum.Price => isDescending
                    ? source.OrderByDescending(item => item.Price).ThenBy(item => item.Name)
                    : source.OrderBy(item => item.Price).ThenBy(item => item.Name),
                PlantComboSortByEnum.Name => isDescending
                    ? source.OrderByDescending(item => item.Name)
                    : source.OrderBy(item => item.Name),
                _ => source.OrderBy(item => item.Name)
            };
        }

        private static string? MapComboTypeName(int? comboType)
        {
            if (!comboType.HasValue || !Enum.IsDefined(typeof(ComboTypeEnum), comboType.Value))
            {
                return null;
            }

            return ((ComboTypeEnum)comboType.Value).ToString();
        }

        #endregion

        #region Cache Management

        private async Task InvalidateCacheAsync()
        {
            await _cacheService.RemoveByPrefixAsync(ALL_COMBOS_KEY);
            await _cacheService.RemoveByPrefixAsync(ACTIVE_COMBOS_KEY);
            await _cacheService.RemoveByPrefixAsync(SHOP_COMBOS_KEY);
            await _cacheService.RemoveByPrefixAsync("shop_unified_search");
        }

        private async Task InvalidateCacheAsync(int? comboId = null, int? nurseryId = null)
        {
            await _cacheService.RemoveByPrefixAsync(ALL_COMBOS_KEY);
            await _cacheService.RemoveByPrefixAsync(ACTIVE_COMBOS_KEY);
            await _cacheService.RemoveByPrefixAsync(SHOP_COMBOS_KEY);
            await _cacheService.RemoveByPrefixAsync("selling_combos_");

            // Assemble/Decompose updates CommonPlant quantities, so clear related inventory/search caches.
            await _cacheService.RemoveByPrefixAsync(ALL_COMMON_PLANTS_KEY);
            await _cacheService.RemoveByPrefixAsync(PLANT_NURSERIES_COMMON_KEY);
            await _cacheService.RemoveByPrefixAsync(PLANT_SHOP_SEARCH_KEY);
            await _cacheService.RemoveByPrefixAsync("nurseries_all_");

            await _cacheService.RemoveByPrefixAsync("shop_unified_search");

            if (nurseryId.HasValue)
            {
                await _cacheService.RemoveByPrefixAsync($"{NURSERY_COMMON_PLANTS_KEY}_{nurseryId.Value}");
            }
            else
            {
                await _cacheService.RemoveByPrefixAsync(NURSERY_COMMON_PLANTS_KEY);
            }

            if (comboId.HasValue)
            {
                await _cacheService.RemoveByPrefixAsync($"{NURSERIES_BY_COMBO_KEY}_{comboId.Value}");
            }
        }

        #endregion

        #region Private Methods

        public async Task<List<NurseryListResponseDto>> GetNurseriesByComboAsync(int comboId)
        {
            var cacheKey = $"{NURSERIES_BY_COMBO_KEY}_{comboId}_v3";
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

            var result = nurseryPlantCombos
                .Where(npc => npc.Nursery != null && npc.Nursery.IsActive == true)
                .OrderByDescending(npc => npc.Id)
                .GroupBy(npc => npc.NurseryId)
                .Select(g => g.First())
                .Select(npc =>
                {
                    var nursery = npc.Nursery!.ToListResponse();
                    nursery.NurseryPlantComboId = npc.Id;
                    nursery.Quantity = npc.Quantity;
                    return nursery;
                })
                .ToList();

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(15));
            return result;
        }

        public async Task<List<PlantComboResponseDto>> GetCompatibleCombosForNurseryAsync(int managerId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("Bạn không phải manager của vựa nào");

            var combos = await _unitOfWork.PlantComboRepository.GetCompatibleCombosForNurseryAsync(nursery.Id);
            return combos.Select(c => c.ToResponse()).ToList();
        }

        #endregion

        #region Embedding Operations

        private void QueueNurseryPlantComboEmbeddingAsync(NurseryPlantCombo entity, PlantCombo? combo = null)
        {
            try
            {
                combo ??= entity.PlantCombo;
                if (combo == null) return;

                var embeddingDto = new NurseryPlantComboEmbeddingDto
                {
                    NurseryPlantComboId = entity.Id,
                    IsActive = entity.IsActive,
                    ComboName = combo.ComboName ?? string.Empty,
                    Description = combo.Description,
                    SuitableSpace = FormatLightRequirementName(combo.SuitableSpace),
                    SuitableRooms = FormatRoomTypeNames(combo.SuitableRooms),
                    FengShuiElement = combo.FengShuiElement,
                    FengShuiPurpose = combo.FengShuiPurpose,
                    ThemeName = combo.ThemeName,
                    ThemeDescription = combo.ThemeDescription,
                    Season = combo.Season.HasValue && Enum.IsDefined(typeof(SeasonTypeEnum), combo.Season.Value)
                        ? (SeasonTypeEnum?)combo.Season.Value
                        : null,
                    PetSafe = combo.PetSafe,
                    ChildSafe = combo.ChildSafe,
                    ComboPrice = combo.ComboPrice,
                    TagNames = combo.TagsNavigation?
                        .Select(t => t.TagName)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToList() ?? new List<string>(),
                    NurseryId = entity.NurseryId,
                    NurseryName = entity.Nursery?.Name,
                    Price = combo.ComboPrice
                };

                var entityId = ConvertToGuid(entity.Id);

                // Queue Hangfire background job for local PostgreSQL
                _backgroundJobClient.Enqueue<IEmbeddingBackgroundJobService>(
                    service => service.ProcessNurseryPlantComboEmbeddingAsync(embeddingDto, entityId, EmbeddingEntityTypes.NurseryPlantCombo));
            }
            catch
            {
                // Log but don't fail the main operation
            }
        }

        private async Task DeleteNurseryPlantComboEmbeddingAsync(int entityId)
        {
            try
            {
                var guid = ConvertToGuid(entityId);
                await _unitOfWork.EmbeddingRepository.DeleteByEntityAsync(EmbeddingEntityTypes.NurseryPlantCombo, guid);
            }
            catch
            {
                // Log but don't fail
            }
        }

        private async Task QueueNurseryPlantComboEmbeddingsByComboIdAsync(int comboId)
        {
            try
            {
                var nurseryPlantCombos = await _unitOfWork.NurseryPlantComboRepository.GetByPlantComboIdForEmbeddingAsync(comboId);
                foreach (var nurseryPlantCombo in nurseryPlantCombos)
                {
                    var entityId = ConvertToGuid(nurseryPlantCombo.Id);
                    _backgroundJobClient.Enqueue<IEmbeddingBackgroundJobService>(
                        service => service.ProcessNurseryPlantComboEmbeddingAsync(
                            nurseryPlantCombo.ToEmbeddingBackfillDto(),
                            entityId,
                            EmbeddingEntityTypes.NurseryPlantCombo));
                }
            }
            catch
            {
                // Re-embedding is best-effort and should not fail PlantCombo write operations.
            }
        }

        private async Task QueueCommonPlantEmbeddingByIdAsync(int commonPlantId)
        {
            try
            {
                var commonPlant = await _unitOfWork.CommonPlantRepository.GetByIdWithDetailsAsync(commonPlantId);
                if (commonPlant == null)
                {
                    return;
                }

                var entityId = ConvertToGuid(commonPlant.Id);
                _backgroundJobClient.Enqueue<IEmbeddingBackgroundJobService>(
                    service => service.ProcessCommonPlantEmbeddingAsync(
                        commonPlant.ToEmbeddingBackfillDto(),
                        entityId,
                        EmbeddingEntityTypes.CommonPlant));
            }
            catch
            {
                // Re-embedding is best-effort and should not fail combo stock operations.
            }
        }

        private static Guid ConvertToGuid(int id)
            => new Guid(id.ToString().PadLeft(32, '0'));

        private static void ValidateEnumBackedFields(int? suitableSpace, List<int>? suitableRooms)
        {
            if (suitableSpace.HasValue && !Enum.IsDefined(typeof(LightRequirementEnum), suitableSpace.Value))
            {
                throw new BadRequestException($"SuitableSpace '{suitableSpace.Value}' không hợp lệ theo LightRequirementEnum");
            }

            if (suitableRooms == null)
            {
                return;
            }

            var invalidRoomTypes = suitableRooms
                .Where(room => !Enum.IsDefined(typeof(RoomTypeEnum), room))
                .Distinct()
                .ToList();

            if (invalidRoomTypes.Any())
            {
                throw new BadRequestException($"SuitableRooms chứa giá trị không hợp lệ theo RoomTypeEnum: {string.Join(", ", invalidRoomTypes)}");
            }
        }

        private static string? FormatLightRequirementName(int? suitableSpace)
        {
            if (!suitableSpace.HasValue || !Enum.IsDefined(typeof(LightRequirementEnum), suitableSpace.Value))
            {
                return null;
            }

            return ((LightRequirementEnum)suitableSpace.Value).ToString();
        }

        private static List<string> FormatRoomTypeNames(List<int>? suitableRooms)
        {
            if (suitableRooms == null || suitableRooms.Count == 0)
            {
                return new List<string>();
            }

            return suitableRooms
                .Distinct()
                .Select(room => Enum.IsDefined(typeof(RoomTypeEnum), room)
                    ? ((RoomTypeEnum)room).ToString()
                    : room.ToString())
                .ToList();
        }

        private static string ExtractCloudinaryPublicId(string? imageUrl)
        {
            if (string.IsNullOrWhiteSpace(imageUrl)) return string.Empty;
            try
            {
                var uri = new Uri(imageUrl);
                var path = uri.AbsolutePath;
                var idx = path.IndexOf("/upload/", StringComparison.Ordinal);
                if (idx < 0) return string.Empty;
                var after = path[(idx + 8)..];
                if (after.StartsWith('v') && after.Contains('/'))
                    after = after[(after.IndexOf('/') + 1)..];
                var dot = after.LastIndexOf('.');
                return dot > 0 ? after[..dot] : after;
            }
            catch { return string.Empty; }
        }

        #endregion
    }
}
