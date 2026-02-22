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
    public class PlantService : IPlantService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string ALL_PLANTS_KEY = "plants_all";
        private const string ACTIVE_PLANTS_KEY = "plants_active";
        private const string SHOP_PLANTS_KEY = "plants_shop";

        public PlantService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        #region CRUD Operations

        public async Task<List<PlantListResponseDto>> GetAllPlantsAsync()
        {
            var cachedData = await _cacheService.GetDataAsync<List<PlantListResponseDto>>(ALL_PLANTS_KEY);
            if (cachedData != null)
                return cachedData;

            var plants = await _unitOfWork.PlantRepository.GetAllWithDetailsAsync();
            var result = plants.ToListResponseList();

            await _cacheService.SetDataAsync(ALL_PLANTS_KEY, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<List<PlantListResponseDto>> GetActivePlantsAsync()
        {
            var cachedData = await _cacheService.GetDataAsync<List<PlantListResponseDto>>(ACTIVE_PLANTS_KEY);
            if (cachedData != null)
                return cachedData;

            var plants = await _unitOfWork.PlantRepository.GetActiveWithDetailsAsync();
            var result = plants.ToListResponseList();

            await _cacheService.SetDataAsync(ACTIVE_PLANTS_KEY, result, DateTimeOffset.Now.AddMinutes(30));
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

        public async Task<List<PlantListResponseDto>> GetPlantsForShopAsync()
        {
            var cachedData = await _cacheService.GetDataAsync<List<PlantListResponseDto>>(SHOP_PLANTS_KEY);
            if (cachedData != null)
                return cachedData;

            var plants = await _unitOfWork.PlantRepository.GetPlantsForShopAsync();
            var result = plants.ToListResponseList();

            await _cacheService.SetDataAsync(SHOP_PLANTS_KEY, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        #endregion

        #region Cache Management

        private async Task InvalidateCacheAsync()
        {
            await _cacheService.RemoveDataAsync(ALL_PLANTS_KEY);
            await _cacheService.RemoveDataAsync(ACTIVE_PLANTS_KEY);
            await _cacheService.RemoveDataAsync(SHOP_PLANTS_KEY);
        }

        #endregion
    }
}
