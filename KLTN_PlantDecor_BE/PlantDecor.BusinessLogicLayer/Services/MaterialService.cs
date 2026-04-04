using Microsoft.EntityFrameworkCore;
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
    public class MaterialService : IMaterialService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ICloudinaryService _cloudinaryService;

        private const string ALL_MATERIALS_KEY = "materials_all";
        private const string ACTIVE_MATERIALS_KEY = "materials_active";
        private const string SHOP_MATERIALS_KEY = "materials_shop";
        private const string NURSERIES_BY_MATERIAL_KEY = "nurseries_by_material";

        public MaterialService(IUnitOfWork unitOfWork, ICacheService cacheService, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _cloudinaryService = cloudinaryService;
        }

        #region CRUD Operations

        public async Task<PaginatedResult<MaterialListResponseDto>> GetAllMaterialsAsync(Pagination pagination)
        {
            var cacheKey = $"{ALL_MATERIALS_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<MaterialListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.MaterialRepository.GetAllWithDetailsAsync(pagination);
            var result = new PaginatedResult<MaterialListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<PaginatedResult<MaterialListResponseDto>> GetActiveMaterialsAsync(Pagination pagination)
        {
            var cacheKey = $"{ACTIVE_MATERIALS_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<MaterialListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.MaterialRepository.GetActiveWithDetailsAsync(pagination);
            var result = new PaginatedResult<MaterialListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<MaterialResponseDto?> GetMaterialByIdAsync(int id)
        {
            var material = await _unitOfWork.MaterialRepository.GetByIdWithDetailsAsync(id);
            if (material == null)
                return null;

            return material.ToResponse();
        }

        public async Task<MaterialResponseDto> CreateMaterialAsync(MaterialRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var materialCode = request.MaterialCode.Trim().ToUpper();
                // Check if material code already exists
                if (!string.IsNullOrEmpty(materialCode))
                {
                    if (await _unitOfWork.MaterialRepository.ExistsByCodeAsync(materialCode))
                        throw new BadRequestException($"Material với mã '{materialCode}' đã tồn tại");
                }

                // Check if material name already exists
                if (!string.IsNullOrWhiteSpace(request.Name))
                {
                    if (await _unitOfWork.MaterialRepository.ExistsByNameAsync(request.Name))
                        throw new BadRequestException($"Material với tên '{request.Name}' đã tồn tại");
                }

                var material = request.ToEntity();
                material.MaterialCode = materialCode; // Ensure code is stored in uppercase

                _unitOfWork.MaterialRepository.PrepareCreate(material);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return material.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<MaterialResponseDto> UpdateMaterialAsync(int id, MaterialUpdateDto request)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var material = await _unitOfWork.MaterialRepository.GetByIdWithDetailsAsync(id);
                if (material == null)
                    throw new NotFoundException($"Material với ID {id} không tồn tại");

                // Check if material code already exists (excluding current material)
                if (!string.IsNullOrEmpty(request.MaterialCode))
                {
                    if (await _unitOfWork.MaterialRepository.ExistsByCodeAsync(request.MaterialCode, id))
                        throw new BadRequestException($"Material với mã '{request.MaterialCode}' đã tồn tại");
                }

                request.ToUpdate(material);

                _unitOfWork.MaterialRepository.PrepareUpdate(material);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return material.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<MaterialResponseDto> UploadMaterialImagesAsync(int materialId, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                throw new BadRequestException("No files were uploaded");

            var material = await _unitOfWork.MaterialRepository.GetByIdWithDetailsAsync(materialId);
            if (material == null)
                throw new NotFoundException($"Material với ID {materialId} không tồn tại");

            foreach (var file in files)
            {
                var (isValid, errorMessage) = _cloudinaryService.ValidateDocumentFile(file);
                if (!isValid)
                    throw new BadRequestException(errorMessage);
            }

            List<FileUploadResponse> uploadedFiles = new();

            try
            {
                uploadedFiles = await _cloudinaryService.UploadFilesAsync(files, "MaterialImages");
                if (uploadedFiles.Count == 0)
                    throw new BadRequestException("Material images upload failed");

                await _unitOfWork.BeginTransactionAsync();

                foreach (var uploadedFile in uploadedFiles)
                {
                    material.MaterialImages.Add(new MaterialImage
                    {
                        MaterialId = material.Id,
                        ImageUrl = uploadedFile.SecureUrl,
                        IsPrimary = false,
                        CreatedAt = DateTime.Now
                    });
                }

                material.UpdatedAt = DateTime.Now;
                _unitOfWork.MaterialRepository.PrepareUpdate(material);

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

            await InvalidateCacheAsync(materialId);

            var updatedMaterial = await _unitOfWork.MaterialRepository.GetByIdWithDetailsAsync(materialId);
            return updatedMaterial!.ToResponse();
        }

        public async Task<MaterialResponseDto> UploadMaterialThumbnailAsync(int materialId, IFormFile file)
        {
            if (file == null)
                throw new BadRequestException("No file was uploaded");

            var material = await _unitOfWork.MaterialRepository.GetByIdWithDetailsAsync(materialId);
            if (material == null)
                throw new NotFoundException($"Material với ID {materialId} không tồn tại");

            var (isValid, errorMessage) = _cloudinaryService.ValidateDocumentFile(file);
            if (!isValid)
                throw new BadRequestException(errorMessage);

            var uploadedFile = await _cloudinaryService.UploadFileAsync(file, "MaterialImages");
            if (uploadedFile == null || string.IsNullOrWhiteSpace(uploadedFile.SecureUrl))
                throw new BadRequestException("Material thumbnail upload failed");

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                foreach (var image in material.MaterialImages)
                {
                    image.IsPrimary = false;
                }

                material.MaterialImages.Add(new MaterialImage
                {
                    MaterialId = material.Id,
                    ImageUrl = uploadedFile.SecureUrl,
                    IsPrimary = true,
                    CreatedAt = DateTime.Now
                });

                material.UpdatedAt = DateTime.Now;
                _unitOfWork.MaterialRepository.PrepareUpdate(material);

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

            await InvalidateCacheAsync(materialId);

            var updatedMaterial = await _unitOfWork.MaterialRepository.GetByIdWithDetailsAsync(materialId);
            return updatedMaterial!.ToResponse();
        }

        public async Task<bool> DeleteMaterialAsync(int id)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var material = await _unitOfWork.MaterialRepository.GetByIdWithOrdersAsync(id);
                if (material == null)
                    throw new NotFoundException($"Material với ID {id} không tồn tại");

                // Check if material has any nursery materials with cart or orders
                var hasActiveOrders = material.NurseryMaterials.Any(nm => nm.CartItems.Any());
                if (hasActiveOrders)
                    throw new BadRequestException("Không thể xóa vật liệu đã có trong đơn hàng. Vui lòng vô hiệu hóa thay vì xóa.");

                // Soft delete by deactivating
                material.IsActive = false;
                material.UpdatedAt = DateTime.Now;

                _unitOfWork.MaterialRepository.PrepareUpdate(material);
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
            var material = await _unitOfWork.MaterialRepository.GetByIdAsync(id);
            if (material == null)
                throw new NotFoundException($"Material với ID {id} không tồn tại");

            material.IsActive = !material.IsActive;
            material.UpdatedAt = DateTime.Now;

            _unitOfWork.MaterialRepository.PrepareUpdate(material);
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync();

            return material.IsActive ?? true;
        }

        #endregion

        #region Category & Tag Assignment

        public async Task<MaterialResponseDto> AssignCategoriesToMaterialAsync(AssignMaterialCategoriesDto request)
        {
            var material = await _unitOfWork.MaterialRepository.GetByIdWithDetailsAsync(request.MaterialId);
            if (material == null)
                throw new NotFoundException($"Material với ID {request.MaterialId} không tồn tại");

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
                material.Categories.Clear();
                foreach (var category in validCategories)
                {
                    material.Categories.Add(category);
                }

                material.UpdatedAt = DateTime.Now;
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return material.ToResponse();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<MaterialResponseDto> AssignTagsToMaterialAsync(AssignMaterialTagsDto request)
        {
            var material = await _unitOfWork.MaterialRepository.GetByIdWithDetailsAsync(request.MaterialId);
            if (material == null)
                throw new NotFoundException($"Material với ID {request.MaterialId} không tồn tại");

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
                material.Tags.Clear();
                foreach (var tag in tags)
                {
                    material.Tags.Add(tag);
                }

                material.UpdatedAt = DateTime.Now;
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();

                await InvalidateCacheAsync();

                return material.ToResponse();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<MaterialResponseDto> RemoveCategoryFromMaterialAsync(int materialId, int categoryId)
        {
            var material = await _unitOfWork.MaterialRepository.GetByIdWithDetailsAsync(materialId);
            if (material == null)
                throw new NotFoundException($"Material với ID {materialId} không tồn tại");

            var category = material.Categories.FirstOrDefault(c => c.Id == categoryId);
            if (category == null)
                throw new NotFoundException($"Material không có category với ID {categoryId}");

            material.Categories.Remove(category);
            material.UpdatedAt = DateTime.Now;
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync();

            return material.ToResponse();
        }

        public async Task<MaterialResponseDto> RemoveTagFromMaterialAsync(int materialId, int tagId)
        {
            var material = await _unitOfWork.MaterialRepository.GetByIdWithDetailsAsync(materialId);
            if (material == null)
                throw new NotFoundException($"Material với ID {materialId} không tồn tại");

            var tag = material.Tags.FirstOrDefault(t => t.Id == tagId);
            if (tag == null)
                throw new NotFoundException($"Material không có tag với ID {tagId}");

            material.Tags.Remove(tag);
            material.UpdatedAt = DateTime.Now;
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync();

            return material.ToResponse();
        }

        #endregion

        #region Shop Display

        public async Task<PaginatedResult<MaterialListResponseDto>> GetMaterialsForShopAsync(Pagination pagination)
        {
            var cacheKey = $"{SHOP_MATERIALS_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<MaterialListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var paginatedEntities = await _unitOfWork.MaterialRepository.GetActiveWithDetailsAsync(pagination);
            var result = new PaginatedResult<MaterialListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<List<NurseryListResponseDto>> GetNurseriesByMaterialAsync(int materialId)
        {
            var cacheKey = $"{NURSERIES_BY_MATERIAL_KEY}_{materialId}_v2";
            var cachedData = await _cacheService.GetDataAsync<List<NurseryListResponseDto>>(cacheKey);
            if (cachedData != null)
                return cachedData;

            var material = await _unitOfWork.MaterialRepository.GetByIdAsync(materialId);
            if (material == null)
            {
                throw new NotFoundException($"Material với ID '{materialId}' không tồn tại");
            }

            var nurseryMaterials = new List<NurseryMaterial>();
            var pageNumber = 1;
            PaginatedResult<NurseryMaterial> pagedResult;

            do
            {
                pagedResult = await _unitOfWork.NurseryMaterialRepository.GetByMaterialIdAsync(
                    materialId,
                    new Pagination(pageNumber, 100)
                );

                nurseryMaterials.AddRange(pagedResult.Items.Where(nm => nm.Quantity > 0 && nm.IsActive));
                pageNumber++;
            }
            while (nurseryMaterials.Count < pagedResult.TotalCount && pagedResult.Items.Any());

            var result = nurseryMaterials
                .Where(nm => nm.Nursery != null && nm.Nursery.IsActive == true)
                .OrderByDescending(nm => nm.Id)
                .GroupBy(nm => nm.NurseryId)
                .Select(g => g.First())
                .Select(nm =>
                {
                    var nursery = nm.Nursery!.ToListResponse();
                    nursery.NurseryMaterialId = nm.Id;
                    return nursery;
                })
                .ToList();

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(15));
            return result;
        }

        #endregion

        #region Private Methods

        private async Task InvalidateCacheAsync(int? materialId = null)
        {
            await _cacheService.RemoveByPrefixAsync(ALL_MATERIALS_KEY);
            await _cacheService.RemoveByPrefixAsync(ACTIVE_MATERIALS_KEY);
            await _cacheService.RemoveByPrefixAsync(SHOP_MATERIALS_KEY);
            await _cacheService.RemoveByPrefixAsync("nurseries_all_");
            await _cacheService.RemoveByPrefixAsync("shop_unified_search");
            if (materialId.HasValue)
            {
                await _cacheService.RemoveByPrefixAsync($"{NURSERIES_BY_MATERIAL_KEY}_{materialId.Value}");
            }
        }

        #endregion
    }
}
