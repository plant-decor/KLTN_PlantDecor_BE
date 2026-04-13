using Hangfire;
using Microsoft.AspNetCore.Http;
using PlantDecor.BusinessLogicLayer.Constants;
using PlantDecor.BusinessLogicLayer.DTOs.Embedding;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class PlantInstanceService : IPlantInstanceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        private const string NURSERY_INSTANCES_KEY = "nursery_instances";
        private const string PLANT_NURSERIES_KEY = "plant_nurseries";
        private const string PLANT_SHOP_SEARCH_KEY = "plants_shop_search";

        public PlantInstanceService(
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

        #region Manager Operations

        public async Task<BatchCreatePlantInstanceResponseDto> BatchCreateAsync(int nurseryId, int managerId, BatchCreatePlantInstanceRequestDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Validate nursery thuộc về manager
                var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
                if (nursery == null || nursery.Id != nurseryId)
                    throw new ForbiddenException("Bạn không có quyền quản lý vựa này");

                // Validate tất cả PlantId tồn tại
                var plantIds = request.Instances.Select(i => i.PlantId).Distinct().ToList();
                foreach (var plantId in plantIds)
                {
                    var plant = await _unitOfWork.PlantRepository.GetByIdAsync(plantId);
                    if (plant == null)
                        throw new NotFoundException($"Plant với ID {plantId} không tồn tại");
                }

                // Tạo các entity
                var entities = request.Instances.ToEntityList(nurseryId);
                await _unitOfWork.PlantInstanceRepository.AddRangeAsync(entities);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                await InvalidateCacheAsync(nurseryId);

                // Queue embeddings for created instances
                foreach (var entity in entities)
                {
                    var reloadedEntity = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(entity.Id);
                    if (reloadedEntity != null)
                    {
                        QueueEmbeddingAsync(reloadedEntity);
                    }
                }

                // Reload với details
                var result = new BatchCreatePlantInstanceResponseDto
                {
                    TotalCreated = entities.Count,
                    Instances = entities.ToResponseList()
                };

                return result;
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PlantInstanceResponseDto> GetByIdAsync(int instanceId, int managerId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("Bạn không có quyền quản lý vựa này");

            var instance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(instanceId);
            if (instance == null)
                throw new NotFoundException($"PlantInstance với ID {instanceId} không tồn tại");

            if (instance.CurrentNurseryId != nursery.Id)
                throw new ForbiddenException("PlantInstance này không thuộc vựa của bạn");

            return instance.ToResponse();
        }

        public async Task<PaginatedResult<PlantInstanceListResponseDto>> GetByNurseryIdAsync(int nurseryId, int managerId, Pagination pagination, int? statusFilter = null)
        {
            // Validate nursery thuộc về manager
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null || nursery.Id != nurseryId)
                throw new ForbiddenException("Bạn không có quyền quản lý vựa này");

            var cacheKey = $"{NURSERY_INSTANCES_KEY}_{nurseryId}_p{pagination.PageNumber}_s{pagination.PageSize}_st{statusFilter}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<PlantInstanceListResponseDto>>(cacheKey);
            if (cachedData != null) return cachedData;

            var paginatedEntities = await _unitOfWork.PlantInstanceRepository.GetByNurseryIdAsync(nurseryId, pagination, statusFilter);
            var result = new PaginatedResult<PlantInstanceListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(15));
            return result;
        }

        public async Task<List<NurseryPlantSummaryDto>> GetPlantsSummaryByNurseryAsync(int nurseryId, int managerId)
        {
            // Validate nursery thuộc về manager
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null || nursery.Id != nurseryId)
                throw new ForbiddenException("Bạn không có quyền quản lý vựa này");

            var cacheKey = $"{NURSERY_INSTANCES_KEY}_{nurseryId}_summary";
            var cachedData = await _cacheService.GetDataAsync<List<NurseryPlantSummaryDto>>(cacheKey);
            if (cachedData != null) return cachedData;

            var instances = await _unitOfWork.PlantInstanceRepository.GetAllByNurseryIdAsync(nurseryId);

            var summary = instances
                .Where(i => i.Plant != null)
                .GroupBy(i => i.PlantId)
                .Select(g =>
                {
                    var plant = g.First().Plant!;
                    return new NurseryPlantSummaryDto
                    {
                        PlantId = plant.Id,
                        PlantName = plant.Name,
                        PrimaryImageUrl = plant.PlantImages?.FirstOrDefault(img => img.IsPrimary == true)?.ImageUrl
                            ?? plant.PlantImages?.FirstOrDefault()?.ImageUrl,
                        BasePrice = plant.BasePrice,
                        TotalInstances = g.Count(),
                        AvailableCount = g.Count(i => i.Status == (int)PlantInstanceStatusEnum.Available),
                        SoldCount = g.Count(i => i.Status == (int)PlantInstanceStatusEnum.Sold),
                        ReservedCount = g.Count(i => i.Status == (int)PlantInstanceStatusEnum.Reserved),
                        DamagedCount = g.Count(i => i.Status == (int)PlantInstanceStatusEnum.Damaged),
                        Inactive = g.Count(i => i.Status == (int)PlantInstanceStatusEnum.Inactive),
                        MinPrice = g.Where(i => i.SpecificPrice.HasValue).Select(i => i.SpecificPrice!.Value).DefaultIfEmpty(0).Min(),
                        MaxPrice = g.Where(i => i.SpecificPrice.HasValue).Select(i => i.SpecificPrice!.Value).DefaultIfEmpty(0).Max()
                    };
                })
                .OrderBy(s => s.PlantName)
                .ToList();

            await _cacheService.SetDataAsync(cacheKey, summary, DateTimeOffset.Now.AddMinutes(15));
            return summary;
        }

        public async Task<PlantInstanceResponseDto> UpdateStatusAsync(int instanceId, int managerId, UpdatePlantInstanceStatusDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                var instance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(instanceId);
                if (instance == null)
                    throw new NotFoundException($"PlantInstance với ID {instanceId} không tồn tại");

                // Validate nursery thuộc về manager
                var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
                if (nursery == null || nursery.Id != instance.CurrentNurseryId)
                    throw new ForbiddenException("Bạn không có quyền quản lý instance này");

                instance.Status = request.Status;
                instance.UpdatedAt = DateTime.Now;
                _unitOfWork.PlantInstanceRepository.PrepareUpdate(instance);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                await InvalidateCacheAsync(instance.CurrentNurseryId);

                // Update embedding
                var reloaded = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(instanceId);
                if (reloaded != null)
                {
                    QueueEmbeddingAsync(reloaded);
                }

                return instance.ToResponse();
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<BatchUpdateStatusResponseDto> BatchUpdateStatusAsync(int managerId, BatchUpdatePlantInstanceStatusDto request)
        {
            await _unitOfWork.BeginTransactionAsync();
            try
            {
                // Validate nursery thuộc về manager
                var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
                if (nursery == null)
                    throw new ForbiddenException("Bạn không có vựa nào");

                var instances = await _unitOfWork.PlantInstanceRepository.GetByIdsAsync(request.InstanceIds);

                if (instances.Count != request.InstanceIds.Count)
                {
                    var foundIds = instances.Select(i => i.Id).ToHashSet();
                    var missingIds = request.InstanceIds.Where(id => !foundIds.Contains(id)).ToList();
                    throw new NotFoundException($"Không tìm thấy PlantInstance với IDs: {string.Join(", ", missingIds)}");
                }

                // Validate tất cả instance thuộc nursery của manager
                var invalidInstances = instances.Where(i => i.CurrentNurseryId != nursery.Id).ToList();
                if (invalidInstances.Any())
                    throw new ForbiddenException($"Bạn không có quyền quản lý instance IDs: {string.Join(", ", invalidInstances.Select(i => i.Id))}");

                foreach (var instance in instances)
                {
                    instance.Status = request.Status;
                    instance.UpdatedAt = DateTime.Now;
                    _unitOfWork.PlantInstanceRepository.PrepareUpdate(instance);
                }

                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
                await InvalidateCacheAsync(nursery.Id);

                // Update embeddings for all updated instances
                foreach (var instance in instances)
                {
                    var reloaded = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(instance.Id);
                    if (reloaded != null)
                    {
                        QueueEmbeddingAsync(reloaded);
                    }
                }

                return new BatchUpdateStatusResponseDto
                {
                    TotalUpdated = instances.Count,
                    Status = request.Status,
                    StatusName = PlantInstanceMapper.GetStatusName(request.Status)
                };
            }
            catch (Exception)
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }
        }

        public async Task<PlantInstanceResponseDto> UploadPlantInstanceImagesAsync(int instanceId, int managerId, List<IFormFile> files)
        {
            if (files == null || files.Count == 0)
                throw new BadRequestException("No files were uploaded");

            var instance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(instanceId);
            if (instance == null)
                throw new NotFoundException($"PlantInstance với ID {instanceId} không tồn tại");

            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null || nursery.Id != instance.CurrentNurseryId)
                throw new ForbiddenException("Bạn không có quyền quản lý instance này");

            if (!instance.PlantId.HasValue)
                throw new BadRequestException("PlantInstance chưa có PlantId hợp lệ");

            foreach (var file in files)
            {
                var (isValid, errorMessage) = _cloudinaryService.ValidateDocumentFile(file);
                if (!isValid)
                    throw new BadRequestException(errorMessage);
            }

            List<FileUploadResponse> uploadedFiles = new();

            try
            {
                uploadedFiles = await _cloudinaryService.UploadFilesAsync(files, "PlantInstanceImages");
                if (uploadedFiles.Count == 0)
                    throw new BadRequestException("Plant instance images upload failed");

                await _unitOfWork.BeginTransactionAsync();

                foreach (var uploadedFile in uploadedFiles)
                {
                    instance.PlantImages.Add(new PlantImage
                    {
                        PlantId = instance.PlantId.Value,
                        PlantInstanceId = instance.Id,
                        ImageUrl = uploadedFile.SecureUrl,
                        IsPrimary = false,
                        CreatedAt = DateTime.Now
                    });
                }

                instance.UpdatedAt = DateTime.Now;
                _unitOfWork.PlantInstanceRepository.PrepareUpdate(instance);

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

            await InvalidateCacheAsync(instance.CurrentNurseryId);

            var updatedInstance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(instanceId);
            return updatedInstance!.ToResponse();
        }

        public async Task<PlantInstanceResponseDto> UploadPlantInstanceThumbnailAsync(int instanceId, int managerId, IFormFile file)
        {
            if (file == null)
                throw new BadRequestException("No file was uploaded");

            var instance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(instanceId);
            if (instance == null)
                throw new NotFoundException($"PlantInstance với ID {instanceId} không tồn tại");

            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null || nursery.Id != instance.CurrentNurseryId)
                throw new ForbiddenException("Bạn không có quyền quản lý instance này");

            if (!instance.PlantId.HasValue)
                throw new BadRequestException("PlantInstance chưa có PlantId hợp lệ");

            var (isValid, errorMessage) = _cloudinaryService.ValidateDocumentFile(file);
            if (!isValid)
                throw new BadRequestException(errorMessage);

            var uploadedFile = await _cloudinaryService.UploadFileAsync(file, "PlantInstanceImages");
            if (uploadedFile == null || string.IsNullOrWhiteSpace(uploadedFile.SecureUrl))
                throw new BadRequestException("Plant instance thumbnail upload failed");

            try
            {
                await _unitOfWork.BeginTransactionAsync();

                foreach (var image in instance.PlantImages)
                {
                    image.IsPrimary = false;
                }

                instance.PlantImages.Add(new PlantImage
                {
                    PlantId = instance.PlantId.Value,
                    PlantInstanceId = instance.Id,
                    ImageUrl = uploadedFile.SecureUrl,
                    IsPrimary = true,
                    CreatedAt = DateTime.Now
                });

                instance.UpdatedAt = DateTime.Now;
                _unitOfWork.PlantInstanceRepository.PrepareUpdate(instance);

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

            await InvalidateCacheAsync(instance.CurrentNurseryId);

            var updatedInstance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(instanceId);
            return updatedInstance!.ToResponse();
        }

        public async Task<PlantInstanceResponseDto> SetPrimaryInstanceImageAsync(int instanceId, int managerId, int imageId)
        {
            var instance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(instanceId);
            if (instance == null)
                throw new NotFoundException($"PlantInstance với ID {instanceId} không tồn tại");

            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null || nursery.Id != instance.CurrentNurseryId)
                throw new ForbiddenException("Bạn không có quyền quản lý instance này");

            var targetImage = instance.PlantImages.FirstOrDefault(i => i.Id == imageId && i.PlantInstanceId == instanceId);
            if (targetImage == null)
                throw new NotFoundException($"Ảnh với ID {imageId} không thuộc instance {instanceId}");

            foreach (var image in instance.PlantImages.Where(i => i.PlantInstanceId == instanceId))
                image.IsPrimary = image.Id == imageId;

            instance.UpdatedAt = DateTime.Now;
            _unitOfWork.PlantInstanceRepository.PrepareUpdate(instance);
            await _unitOfWork.SaveAsync();

            await InvalidateCacheAsync(instance.CurrentNurseryId);

            var updatedInstance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(instanceId);
            return updatedInstance!.ToResponse();
        }

        public async Task<PlantInstanceResponseDto> ReplaceInstanceImageAsync(int instanceId, int managerId, int imageId, IFormFile file)
        {
            if (file == null)
                throw new BadRequestException("No file was uploaded");

            var instance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(instanceId);
            if (instance == null)
                throw new NotFoundException($"PlantInstance với ID {instanceId} không tồn tại");

            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null || nursery.Id != instance.CurrentNurseryId)
                throw new ForbiddenException("Bạn không có quyền quản lý instance này");

            var image = instance.PlantImages.FirstOrDefault(i => i.Id == imageId && i.PlantInstanceId == instanceId);
            if (image == null)
                throw new NotFoundException($"Ảnh với ID {imageId} không thuộc instance {instanceId}");

            var (isValid, errorMessage) = _cloudinaryService.ValidateDocumentFile(file);
            if (!isValid)
                throw new BadRequestException(errorMessage);

            var oldPublicId = ExtractCloudinaryPublicId(image.ImageUrl);

            var uploadedFile = await _cloudinaryService.UploadFileAsync(file, "PlantInstanceImages");
            if (uploadedFile == null || string.IsNullOrWhiteSpace(uploadedFile.SecureUrl))
                throw new BadRequestException("Image upload failed");

            image.ImageUrl = uploadedFile.SecureUrl;
            instance.UpdatedAt = DateTime.Now;
            _unitOfWork.PlantInstanceRepository.PrepareUpdate(instance);
            await _unitOfWork.SaveAsync();

            if (!string.IsNullOrWhiteSpace(oldPublicId))
            {
                try { await _cloudinaryService.DeleteFileAsync(oldPublicId); }
                catch { }
            }

            await InvalidateCacheAsync(instance.CurrentNurseryId);

            var updatedInstance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(instanceId);
            return updatedInstance!.ToResponse();
        }

        public async Task<PlantInstanceResponseDto> DeleteInstanceImageAsync(int instanceId, int managerId, int imageId)
        {
            var instance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(instanceId);
            if (instance == null)
                throw new NotFoundException($"PlantInstance với ID {instanceId} không tồn tại");

            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null || nursery.Id != instance.CurrentNurseryId)
                throw new ForbiddenException("Bạn không có quyền quản lý instance này");

            var image = instance.PlantImages.FirstOrDefault(i => i.Id == imageId && i.PlantInstanceId == instanceId);
            if (image == null)
                throw new NotFoundException($"Ảnh với ID {imageId} không thuộc instance {instanceId}");

            var wasPrimary = image.IsPrimary == true;
            var publicId = ExtractCloudinaryPublicId(image.ImageUrl);

            instance.PlantImages.Remove(image);

            if (wasPrimary)
            {
                var next = instance.PlantImages.FirstOrDefault(i => i.PlantInstanceId == instanceId);
                if (next != null) next.IsPrimary = true;
            }

            instance.UpdatedAt = DateTime.Now;
            _unitOfWork.PlantInstanceRepository.PrepareUpdate(instance);
            await _unitOfWork.SaveAsync();

            if (!string.IsNullOrWhiteSpace(publicId))
            {
                try { await _cloudinaryService.DeleteFileAsync(publicId); }
                catch { }
            }

            await InvalidateCacheAsync(instance.CurrentNurseryId);

            var updatedInstance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(instanceId);
            return updatedInstance!.ToResponse();
        }

        #endregion

        #region Shop Operations

        public async Task<List<PlantNurseryAvailabilityDto>> GetNurseriesByPlantIdAsync(int plantId)
        {
            var cacheKey = $"{PLANT_NURSERIES_KEY}_{plantId}";
            var cachedData = await _cacheService.GetDataAsync<List<PlantNurseryAvailabilityDto>>(cacheKey);
            if (cachedData != null) return cachedData;

            // Validate plant tồn tại
            var plant = await _unitOfWork.PlantRepository.GetByIdAsync(plantId);
            if (plant == null)
                throw new NotFoundException($"Plant với ID {plantId} không tồn tại");

            var instances = await _unitOfWork.PlantInstanceRepository.GetAvailableByPlantIdAsync(plantId);

            var result = instances
                .Where(i => i.CurrentNursery != null && i.CurrentNursery.IsActive == true)
                .GroupBy(i => i.CurrentNurseryId)
                .Select(g =>
                {
                    var nursery = g.First().CurrentNursery!;
                    return new PlantNurseryAvailabilityDto
                    {
                        NurseryId = nursery.Id,
                        NurseryName = nursery.Name,
                        Address = nursery.Address,
                        Phone = nursery.Phone,
                        Latitude = nursery.Latitude,
                        Longitude = nursery.Longitude,
                        AvailableInstanceCount = g.Count(),
                        MinPrice = g.Where(i => i.SpecificPrice.HasValue).Select(i => i.SpecificPrice!.Value).DefaultIfEmpty(0).Min(),
                        MaxPrice = g.Where(i => i.SpecificPrice.HasValue).Select(i => i.SpecificPrice!.Value).DefaultIfEmpty(0).Max()
                    };
                })
                .OrderByDescending(n => n.AvailableInstanceCount)
                .ToList();

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(10));
            return result;
        }

        public async Task<PaginatedResult<PlantInstanceListResponseDto>> GetAvailableByNurseryIdAsync(int nurseryId, Pagination pagination, int? plantId = null)
        {
            var plantPart = plantId.HasValue ? plantId.Value.ToString() : "all";
            var cacheKey = $"{NURSERY_INSTANCES_KEY}_{nurseryId}_available_pid{plantPart}_p{pagination.PageNumber}_s{pagination.PageSize}";
            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<PlantInstanceListResponseDto>>(cacheKey);
            if (cachedData != null) return cachedData;

            var paginatedEntities = await _unitOfWork.PlantInstanceRepository.GetAvailableByNurseryIdAsync(nurseryId, pagination, plantId);
            var result = new PaginatedResult<PlantInstanceListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(10));
            return result;
        }

        public async Task<PlantInstanceResponseDto> GetInstanceDetailAsync(int instanceId)
        {
            var instance = await _unitOfWork.PlantInstanceRepository.GetByIdWithDetailsAsync(instanceId);
            if (instance == null)
                throw new NotFoundException($"PlantInstance với ID {instanceId} không tồn tại");

            if (instance.Status != (int)PlantInstanceStatusEnum.Available)
                throw new NotFoundException($"PlantInstance với ID {instanceId} không khả dụng");

            return instance.ToResponse();
        }

        public async Task<PaginatedResult<PlantInstanceListResponseDto>> SearchAvailableForShopAsync(Pagination pagination, int? nurseryId = null, int? plantId = null)
        {
            var nurseryPart = nurseryId.HasValue ? nurseryId.Value.ToString() : "all";
            var plantPart = plantId.HasValue ? plantId.Value.ToString() : "all";
            var cacheKey = $"{NURSERY_INSTANCES_KEY}_shop_search_n{nurseryPart}_pid{plantPart}_p{pagination.PageNumber}_s{pagination.PageSize}";

            var cachedData = await _cacheService.GetDataAsync<PaginatedResult<PlantInstanceListResponseDto>>(cacheKey);
            if (cachedData != null) return cachedData;

            var paginatedEntities = await _unitOfWork.PlantInstanceRepository.GetAvailableForShopAsync(pagination, nurseryId, plantId);
            var result = new PaginatedResult<PlantInstanceListResponseDto>(
                paginatedEntities.Items.ToListResponseList(),
                paginatedEntities.TotalCount,
                paginatedEntities.PageNumber,
                paginatedEntities.PageSize
            );

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(10));
            return result;
        }

        #endregion

        #region Cache Management

        private async Task InvalidateCacheAsync(int? nurseryId = null)
        {
            await _cacheService.RemoveByPrefixAsync(PLANT_NURSERIES_KEY);
            await _cacheService.RemoveByPrefixAsync(PLANT_SHOP_SEARCH_KEY);
            await _cacheService.RemoveByPrefixAsync("nurseries_all_");
            await _cacheService.RemoveByPrefixAsync("shop_unified_search");
            if (nurseryId.HasValue)
            {
                await _cacheService.RemoveByPrefixAsync($"{NURSERY_INSTANCES_KEY}_{nurseryId.Value}");
            }
            else
            {
                await _cacheService.RemoveByPrefixAsync(NURSERY_INSTANCES_KEY);
            }
        }

        #endregion

        #region Private Helpers

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

        #region Embedding Operations

        private void QueueEmbeddingAsync(PlantInstance entity)
        {
            try
            {
                var plant = entity.Plant;
                var guide = plant?.PlantGuide;

                var embeddingDto = new PlantInstanceEmbeddingDto
                {
                    PlantInstanceId = entity.Id,
                    PlantId = entity.PlantId ?? 0,
                    Status = entity.Status,
                    PlantName = plant?.Name ?? string.Empty,
                    PlantSpecificName = plant?.SpecificName,
                    FengShuiElement = plant?.FengShuiElement,
                    FengShuiMeaning = plant?.FengShuiMeaning,
                    PetSafe = plant?.PetSafe,
                    ChildSafe = plant?.ChildSafe,
                    AirPurifying = plant?.AirPurifying,
                    Description = entity.Description,
                    HealthStatus = entity.HealthStatus,
                    Height = entity.Height,
                    TrunkDiameter = entity.TrunkDiameter,
                    Age = entity.Age,
                    SKU = entity.SKU,
                    Price = entity.SpecificPrice,
                    SpecificPrice = entity.SpecificPrice,
                    BasePrice = plant?.BasePrice,
                    CategoryNames = plant?.Categories?
                        .Select(c => c.Name)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToList() ?? new List<string>(),
                    TagNames = plant?.Tags?
                        .Select(t => t.TagName)
                        .Where(n => !string.IsNullOrWhiteSpace(n))
                        .ToList() ?? new List<string>(),
                    NurseryId = entity.CurrentNurseryId ?? 0,
                    NurseryName = entity.CurrentNursery?.Name,
                    GuideLightRequirement = guide?.LightRequirement
                };

                var entityId = ConvertToGuid(entity.Id);

                // Queue Hangfire background job for local PostgreSQL
                _backgroundJobClient.Enqueue<IEmbeddingBackgroundJobService>(
                    service => service.ProcessPlantInstanceEmbeddingAsync(embeddingDto, entityId, EmbeddingEntityTypes.PlantInstance));
            }
            catch
            {
                // Log but don't fail the main operation
            }
        }

        private async Task DeleteEmbeddingAsync(int entityId)
        {
            try
            {
                var guid = ConvertToGuid(entityId);
                await _unitOfWork.EmbeddingRepository.DeleteByEntityAsync(EmbeddingEntityTypes.PlantInstance, guid);
            }
            catch
            {
                // Log but don't fail
            }
        }

        private static Guid ConvertToGuid(int id)
            => new Guid(id.ToString().PadLeft(32, '0'));

        #endregion
    }
}
