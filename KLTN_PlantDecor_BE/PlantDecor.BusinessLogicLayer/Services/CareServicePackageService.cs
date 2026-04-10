using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class CareServicePackageService : ICareServicePackageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string CACHE_KEY_ACTIVE = "care_pkg_active";
        private const string CACHE_KEY_ALL = "care_pkg_all";
        private const string CACHE_KEY_PREFIX = "care_pkg";

        public CareServicePackageService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<List<CareServicePackageResponseDto>> GetAllActiveAsync()
        {
            var cached = await _cacheService.GetDataAsync<List<CareServicePackageResponseDto>>(CACHE_KEY_ACTIVE);
            if (cached != null) return cached;

            var packages = await _unitOfWork.CareServicePackageRepository.GetAllActiveAsync();
            var result = packages.Select(MapToDto).ToList();
            await _cacheService.SetDataAsync(CACHE_KEY_ACTIVE, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<List<CareServicePackageResponseDto>> GetAllAsync()
        {
            var cached = await _cacheService.GetDataAsync<List<CareServicePackageResponseDto>>(CACHE_KEY_ALL);
            if (cached != null) return cached;

            var packages = await _unitOfWork.CareServicePackageRepository.GetAllAsync();
            var result = packages.OrderBy(p => p.Id).Select(MapToDto).ToList();
            await _cacheService.SetDataAsync(CACHE_KEY_ALL, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<CareServicePackageResponseDto> GetByIdAsync(int id)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}_{id}";
            var cached = await _cacheService.GetDataAsync<CareServicePackageResponseDto>(cacheKey);
            if (cached != null) return cached;

            var pkg = await _unitOfWork.CareServicePackageRepository.GetByIdWithDetailsAsync(id);
            if (pkg == null)
                throw new NotFoundException($"CareServicePackage {id} not found");

            var result = MapToDto(pkg);
            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<CareServicePackageResponseDto> CreateAsync(CreateCareServicePackageRequestDto request)
        {
            if (await _unitOfWork.CareServicePackageRepository.ExistsByNameAsync(request.Name))
                throw new BadRequestException($"A package named '{request.Name}' already exists");

            if (request.ServiceType == (int)CareServiceTypeEnum.OneTime)
            {
                request.VisitPerWeek = null;
                request.DurationDays = 1;
            }
            else if (request.ServiceType == (int)CareServiceTypeEnum.Periodic &&
                (!request.VisitPerWeek.HasValue || request.VisitPerWeek.Value == 0))
            {
                throw new BadRequestException("VisitPerWeek (1-7) is required for Periodic service packages");
            }

            var pkg = new CareServicePackage
            {
                Name = request.Name,
                Description = request.Description,
                Features = request.Features,
                VisitPerWeek = request.VisitPerWeek,
                DurationDays = request.DurationDays,
                ServiceType = request.ServiceType,
                AreaLimit = request.AreaLimit,
                UnitPrice = request.UnitPrice,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _unitOfWork.CareServicePackageRepository.PrepareCreate(pkg);
            await _unitOfWork.SaveAsync();

            if (request.SpecializationIds != null && request.SpecializationIds.Count > 0)
                await _unitOfWork.CareServicePackageRepository.AddSpecializationsAsync(pkg.Id, request.SpecializationIds);

            await InvalidateCacheAsync();

            var created = await _unitOfWork.CareServicePackageRepository.GetByIdWithDetailsAsync(pkg.Id);
            return MapToDto(created!);
        }

        public async Task<CareServicePackageResponseDto> UpdateAsync(int id, UpdateCareServicePackageRequestDto request)
        {
            var pkg = await _unitOfWork.CareServicePackageRepository.GetByIdAsync(id);
            if (pkg == null)
                throw new NotFoundException($"CareServicePackage {id} not found");

            if (request.Name != null && request.Name != pkg.Name)
            {
                if (await _unitOfWork.CareServicePackageRepository.ExistsByNameAsync(request.Name, id))
                    throw new BadRequestException($"A package named '{request.Name}' already exists");
                pkg.Name = request.Name;
            }

            if (request.Description != null) pkg.Description = request.Description;
            if (request.Features != null) pkg.Features = request.Features;
            if (request.VisitPerWeek.HasValue) pkg.VisitPerWeek = request.VisitPerWeek;
            if (request.DurationDays.HasValue) pkg.DurationDays = request.DurationDays;
            if (request.ServiceType.HasValue) pkg.ServiceType = request.ServiceType;
            if (request.AreaLimit.HasValue) pkg.AreaLimit = request.AreaLimit;
            if (request.UnitPrice.HasValue) pkg.UnitPrice = request.UnitPrice;
            if (request.IsActive.HasValue) pkg.IsActive = request.IsActive;

            _unitOfWork.CareServicePackageRepository.PrepareUpdate(pkg);
            await _unitOfWork.SaveAsync();
            await InvalidateCacheAsync();

            var updated = await _unitOfWork.CareServicePackageRepository.GetByIdWithDetailsAsync(id);
            return MapToDto(updated!);
        }

        public async Task DeleteAsync(int id)
        {
            var pkg = await _unitOfWork.CareServicePackageRepository.GetByIdAsync(id);
            if (pkg == null)
                throw new NotFoundException($"CareServicePackage {id} not found");

            // Soft delete
            pkg.IsActive = false;
            _unitOfWork.CareServicePackageRepository.PrepareUpdate(pkg);
            await _unitOfWork.SaveAsync();
            await InvalidateCacheAsync();
        }

        public async Task<List<CareServicePackageResponseDto>> GetPackagesWithNurseriesAsync()
        {
            var packages = await _unitOfWork.CareServicePackageRepository.GetPackagesWithNurseriesAsync();
            return packages.Select(MapToDto).ToList();
        }

        public async Task<List<CareServicePackageResponseDto>> GetNotOfferedByManagerAsync(int managerId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

            var packages = await _unitOfWork.CareServicePackageRepository.GetNotActivelyOfferedByNurseryAsync(nursery.Id);
            return packages.Select(MapToDto).ToList();
        }

        private async Task InvalidateCacheAsync()
        {
            await _cacheService.RemoveByPrefixAsync(CACHE_KEY_PREFIX);
        }

        public static CareServicePackageResponseDto MapToDto(CareServicePackage pkg)
        {
            int? totalSessions = null;
            if (pkg.VisitPerWeek.HasValue && pkg.DurationDays.HasValue)
                totalSessions = (int)Math.Ceiling(pkg.DurationDays.Value / 7.0) * pkg.VisitPerWeek.Value;

            return new CareServicePackageResponseDto
            {
                Id = pkg.Id,
                Name = pkg.Name,
                Description = pkg.Description,
                Features = pkg.Features,
                VisitPerWeek = pkg.VisitPerWeek,
                DurationDays = pkg.DurationDays,
                TotalSessions = totalSessions,
                ServiceType = pkg.ServiceType,
                AreaLimit = pkg.AreaLimit,
                UnitPrice = pkg.UnitPrice,
                IsActive = pkg.IsActive,
                CreatedAt = pkg.CreatedAt,
                Specializations = pkg.CareServiceSpecializations
                    .Where(cs => cs.Specialization != null)
                    .Select(cs => new SpecializationSummaryDto
                    {
                        Id = cs.Specialization.Id,
                        Name = cs.Specialization.Name,
                        Description = cs.Specialization.Description
                    }).ToList()
            };
        }
    }
}
