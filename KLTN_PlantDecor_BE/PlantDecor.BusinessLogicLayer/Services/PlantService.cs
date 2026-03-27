using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
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
    public class PlantService : IPlantService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<PlantService> _logger;

        private const string ALL_PLANTS_KEY = "plants_all";
        private const string ACTIVE_PLANTS_KEY = "plants_active";
        private const string SHOP_PLANTS_KEY = "plants_shop";

        public PlantService(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            ICloudinaryService cloudinaryService,
            ILogger<PlantService> logger)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        #region CRUD Operations

        public async Task<PaginatedResult<PlantListResponseDto>> GetAllPlantsAsync(Pagination pagination)
        {
            var cacheKey = $"{ALL_PLANTS_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<PlantListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.PlantRepository.GetAllWithDetailsAsync(pagination);
            var result = new PaginatedResult<PlantListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<PaginatedResult<PlantListResponseDto>> SearchAllPlantsAsync(PlantSearchRequestDto request)
        {
            var pagination = request?.Pagination ?? new Pagination();
            var filter = BuildSearchFilter(request);

            var cacheKey = BuildSearchCacheKey("plants_system_search", filter, pagination);
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<PlantListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.PlantRepository.SearchAllWithDetailsAsync(filter, pagination);
            var result = new PaginatedResult<PlantListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<PaginatedResult<PlantListResponseDto>> GetActivePlantsAsync(Pagination pagination)
        {
            var cacheKey = $"{ACTIVE_PLANTS_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<PlantListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.PlantRepository.GetActiveWithDetailsAsync(pagination);
            var result = new PaginatedResult<PlantListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<PlantResponseDto?> GetPlantByIdAsync(int id)
        {
            var plant = await _unitOfWork.PlantRepository.GetByIdWithDetailsAsync(id);
            if (plant == null)
                return null;

            return plant.ToResponse();
        }

        public async Task<PlantResponseDto> CreatePlantAsync(PlantRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                // Check if plant name already exists
                if (await _unitOfWork.PlantRepository.ExistsByNameAsync(request.Name))
                    throw new BadRequestException($"Plant với tên '{request.Name}' đã tồn tại");

                var plant = request.ToEntity();

                _unitOfWork.PlantRepository.PrepareCreate(plant);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return plant.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PlantResponseDto> UpdatePlantAsync(int id, PlantUpdateDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var plant = await _unitOfWork.PlantRepository.GetByIdWithDetailsAsync(id);
                if (plant == null)
                    throw new NotFoundException($"Plant với ID {id} không tồn tại");

                // Check if plant name already exists (excluding current plant)
                if (await _unitOfWork.PlantRepository.ExistsByNameAsync(request.Name, id))
                    throw new BadRequestException($"Plant với tên '{request.Name}' đã tồn tại");

                request.ToUpdate(plant);

                _unitOfWork.PlantRepository.PrepareUpdate(plant);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return plant.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PlantResponseDto> UploadPlantImagesAsync(int plantId, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                throw new BadRequestException("No files were uploaded");

            var plant = await _unitOfWork.PlantRepository.GetByIdWithDetailsAsync(plantId);
            if (plant == null)
                throw new NotFoundException($"Plant với ID {plantId} không tồn tại");

            foreach (var file in files)
            {
                var (isValid, errorMessage) = _cloudinaryService.ValidateDocumentFile(file);
                if (!isValid)
                {
                    throw new BadRequestException(errorMessage);
                }
            }

            var uploadedFiles = await _cloudinaryService.UploadFilesAsync(files, "PlantImages");
            if (uploadedFiles == null || uploadedFiles.Count == 0)
            {
                throw new BadRequestException("Plant images upload failed");
            }

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                var hasPrimary = plant.PlantImages.Any(i => i.IsPrimary == true);
                foreach (var uploadedFile in uploadedFiles)
                {
                    plant.PlantImages.Add(new PlantImage
                    {
                        PlantId = plant.Id,
                        ImageUrl = uploadedFile.SecureUrl,
                        IsPrimary = !hasPrimary,
                        CreatedAt = DateTime.Now
                    });

                    if (!hasPrimary)
                    {
                        hasPrimary = true;
                    }
                }

                plant.UpdatedAt = DateTime.Now;
                _unitOfWork.PlantRepository.PrepareUpdate(plant);

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
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to cleanup uploaded plant image: {PublicId}", uploadedFile.PublicId);
                    }
                }

                throw;
            }

            await InvalidateCacheAsync();

            var updatedPlant = await _unitOfWork.PlantRepository.GetByIdWithDetailsAsync(plantId);
            return updatedPlant!.ToResponse();
        }

        public async Task<PlantResponseDto> SetPrimaryPlantImageAsync(int plantId, int imageId)
        {
            var plant = await _unitOfWork.PlantRepository.GetByIdWithDetailsAsync(plantId);
            if (plant == null)
                throw new NotFoundException($"Plant với ID {plantId} không tồn tại");

            var targetImage = plant.PlantImages.FirstOrDefault(i => i.Id == imageId);
            if (targetImage == null)
                throw new NotFoundException($"Ảnh với ID {imageId} không thuộc plant {plantId}");

            foreach (var image in plant.PlantImages)
            {
                image.IsPrimary = image.Id == imageId;
            }

            plant.UpdatedAt = DateTime.Now;
            _unitOfWork.PlantRepository.PrepareUpdate(plant);
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync();

            var updatedPlant = await _unitOfWork.PlantRepository.GetByIdWithDetailsAsync(plantId);
            return updatedPlant!.ToResponse();
        }

        public async Task<bool> DeletePlantAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var plant = await _unitOfWork.PlantRepository.GetByIdWithInstancesAsync(id);
                if (plant == null)
                    throw new NotFoundException($"Plant với ID {id} không tồn tại");

                // Check if plant has instances
                if (plant.PlantInstances.Any())
                    throw new BadRequestException("Không thể xóa plant có instance. Vui lòng xóa các instance trước.");

                // Soft delete by deactivating
                plant.IsActive = false;
                plant.UpdatedAt = DateTime.Now;

                _unitOfWork.PlantRepository.PrepareUpdate(plant);
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
            var plant = await _unitOfWork.PlantRepository.GetByIdAsync(id);
            if (plant == null)
                throw new NotFoundException($"Plant với ID {id} không tồn tại");

            plant.IsActive = !plant.IsActive;
            plant.UpdatedAt = DateTime.Now;

            _unitOfWork.PlantRepository.PrepareUpdate(plant);
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync();

            return plant.IsActive ?? true;
        }

        #endregion

        #region Category & Tag Assignment

        public async Task<PlantResponseDto> AssignCategoriesToPlantAsync(AssignCategoriesDto request)
        {
            var plant = await _unitOfWork.PlantRepository.GetByIdWithDetailsAsync(request.PlantId);
            if (plant == null)
                throw new NotFoundException($"Plant với ID {request.PlantId} không tồn tại");

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
                plant.Categories.Clear();
                foreach (var category in validCategories)
                {
                    plant.Categories.Add(category);
                }

                plant.UpdatedAt = DateTime.Now;
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return plant.ToResponse();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PlantResponseDto> AssignTagsToPlantAsync(AssignTagsDto request)
        {
            var plant = await _unitOfWork.PlantRepository.GetByIdWithDetailsAsync(request.PlantId);
            if (plant == null)
                throw new NotFoundException($"Plant với ID {request.PlantId} không tồn tại");

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
                plant.Tags.Clear();
                foreach (var tag in tags)
                {
                    plant.Tags.Add(tag);
                }

                plant.UpdatedAt = DateTime.Now;
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return plant.ToResponse();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PlantResponseDto> RemoveCategoryFromPlantAsync(int plantId, int categoryId)
        {
            var plant = await _unitOfWork.PlantRepository.GetByIdWithDetailsAsync(plantId);
            if (plant == null)
                throw new NotFoundException($"Plant với ID {plantId} không tồn tại");

            var category = plant.Categories.FirstOrDefault(c => c.Id == categoryId);
            if (category == null)
                throw new NotFoundException($"Plant không có category với ID {categoryId}");

            plant.Categories.Remove(category);
            plant.UpdatedAt = DateTime.Now;
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync();

            return plant.ToResponse();
        }

        public async Task<PlantResponseDto> RemoveTagFromPlantAsync(int plantId, int tagId)
        {
            var plant = await _unitOfWork.PlantRepository.GetByIdWithDetailsAsync(plantId);
            if (plant == null)
                throw new NotFoundException($"Plant với ID {plantId} không tồn tại");

            var tag = plant.Tags.FirstOrDefault(t => t.Id == tagId);
            if (tag == null)
                throw new NotFoundException($"Plant không có tag với ID {tagId}");

            plant.Tags.Remove(tag);
            plant.UpdatedAt = DateTime.Now;
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync();

            return plant.ToResponse();
        }

        #endregion

        #region Shop Display

        public async Task<PaginatedResult<PlantListResponseDto>> GetPlantsForShopAsync(Pagination pagination)
        {
            var cacheKey = $"{SHOP_PLANTS_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<PlantListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.PlantRepository.GetPlantsForShopAsync(pagination);
            var result = new PaginatedResult<PlantListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<PaginatedResult<PlantListResponseDto>> SearchPlantsForShopAsync(PlantSearchRequestDto request)
        {
            var pagination = request?.Pagination ?? new Pagination();
            var filter = BuildSearchFilter(request);

            var cacheKey = BuildSearchCacheKey("plants_shop_search", filter, pagination);
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<PlantListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.PlantRepository.SearchForShopAsync(filter, pagination);
            var result = new PaginatedResult<PlantListResponseDto>(
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
            await _cacheService.RemoveByPrefixAsync(ALL_PLANTS_KEY);
            await _cacheService.RemoveByPrefixAsync(ACTIVE_PLANTS_KEY);
            await _cacheService.RemoveByPrefixAsync(SHOP_PLANTS_KEY);
            await _cacheService.RemoveByPrefixAsync("plants_system_search");
            await _cacheService.RemoveByPrefixAsync("plants_shop_search");
            await _cacheService.RemoveByPrefixAsync("nursery_common_plants");
            await _cacheService.RemoveByPrefixAsync("common_plants_all");
            await _cacheService.RemoveByPrefixAsync("plant_nurseries_common");
            await _cacheService.RemoveByPrefixAsync("nursery_instances");
            await _cacheService.RemoveByPrefixAsync("plant_nurseries");
            await _cacheService.RemoveByPrefixAsync("combos_all");
            await _cacheService.RemoveByPrefixAsync("combos_active");
            await _cacheService.RemoveByPrefixAsync("combos_shop");
            await _cacheService.RemoveByPrefixAsync("nurseries_all_");
        }

        private static PlantSearchFilter BuildSearchFilter(PlantSearchRequestDto? request)
        {
            return new PlantSearchFilter
            {
                Keyword = request?.Keyword,
                IsActive = request?.IsActive,
                PlacementType = request?.PlacementType,
                CareLevelType = request?.CareLevelType,
                CareLevel = request?.CareLevel,
                Toxicity = request?.Toxicity,
                AirPurifying = request?.AirPurifying,
                HasFlower = request?.HasFlower,
                PetSafe = request?.PetSafe,
                ChildSafe = request?.ChildSafe,
                IsUniqueInstance = request?.IsUniqueInstance,
                MinBasePrice = request?.MinBasePrice,
                MaxBasePrice = request?.MaxBasePrice,
                CategoryIds = request?.CategoryIds,
                TagIds = request?.TagIds,
                Sizes = request?.Sizes,
                FengShuiElement = request?.FengShuiElement,
                NurseryId = request?.NurseryId,
                SortBy = request?.SortBy,
                SortDirection = request?.SortDirection
            };
        }

        private static string BuildSearchCacheKey(string prefix, PlantSearchFilter filter, Pagination pagination)
        {
            var categoryPart = filter.CategoryIds == null || filter.CategoryIds.Count == 0
                ? "none"
                : string.Join("-", filter.CategoryIds.OrderBy(x => x));

            var tagPart = filter.TagIds == null || filter.TagIds.Count == 0
                ? "none"
                : string.Join("-", filter.TagIds.OrderBy(x => x));

            var sizePart = filter.Sizes == null || filter.Sizes.Count == 0
                ? "none"
                : string.Join("-", filter.Sizes.OrderBy(x => x));

            var fengShuiPart = string.IsNullOrWhiteSpace(filter.FengShuiElement)
                ? "none"
                : filter.FengShuiElement.Trim().ToLowerInvariant();

            return $"{prefix}_p{pagination.PageNumber}_s{pagination.PageSize}_k{filter.Keyword}_a{filter.IsActive}_pt{filter.PlacementType}_clt{filter.CareLevelType}_cl{filter.CareLevel}_tx{filter.Toxicity}_ap{filter.AirPurifying}_hf{filter.HasFlower}_ps{filter.PetSafe}_cs{filter.ChildSafe}_ui{filter.IsUniqueInstance}_min{filter.MinBasePrice}_max{filter.MaxBasePrice}_cat{categoryPart}_tag{tagPart}_sz{sizePart}_fe{fengShuiPart}_n{filter.NurseryId}_sb{filter.SortBy}_sd{filter.SortDirection}";
        }

        #endregion
    }
}
