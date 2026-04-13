using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class NurseryCareServiceService : INurseryCareServiceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string CACHE_KEY_PREFIX = "nursery_care_svc";

        public NurseryCareServiceService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<List<NurseryCareServiceResponseDto>> GetActiveByNurseryIdAsync(int nurseryId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByIdAsync(nurseryId);
            if (nursery == null)
                throw new NotFoundException($"Nursery {nurseryId} not found");

            var cacheKey = $"{CACHE_KEY_PREFIX}_{nurseryId}_active";
            var cached = await _cacheService.GetDataAsync<List<NurseryCareServiceResponseDto>>(cacheKey);
            if (cached != null) return cached;

            var items = await _unitOfWork.NurseryCareServiceRepository.GetByNurseryIdAsync(nurseryId);
            var result = items.Select(MapToDto).ToList();
            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<List<NurseryCareServiceResponseDto>> GetActiveByManagerAsync(int managerId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

            var items = await _unitOfWork.NurseryCareServiceRepository.GetByNurseryIdAsync(nursery.Id);
            return items.Select(MapToDto).ToList();
        }

        public async Task<List<NurseryCareServiceResponseDto>> GetAllByManagerAsync(int managerId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

            var cacheKey = $"{CACHE_KEY_PREFIX}_{nursery.Id}_all";
            var cached = await _cacheService.GetDataAsync<List<NurseryCareServiceResponseDto>>(cacheKey);
            if (cached != null) return cached;

            var items = await _unitOfWork.NurseryCareServiceRepository.GetAllByNurseryIdAsync(nursery.Id);
            var result = items.Select(MapToDto).ToList();
            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<NurseryCareServiceResponseDto> AddToNurseryAsync(int managerId, CreateNurseryCareServiceRequestDto request)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

            var package = await _unitOfWork.CareServicePackageRepository.GetByIdAsync(request.CareServicePackageId);
            if (package == null)
                throw new NotFoundException($"CareServicePackage {request.CareServicePackageId} not found");

            if (package.IsActive != true)
                throw new BadRequestException("This care service package is not active");

            var alreadyExists = await _unitOfWork.NurseryCareServiceRepository
                .ExistsByNurseryAndPackageAsync(nursery.Id, request.CareServicePackageId);
            if (alreadyExists)
                throw new BadRequestException("This package has already been added to your nursery");

            var nurseryCareService = new NurseryCareService
            {
                NurseryId = nursery.Id,
                CareServicePackageId = request.CareServicePackageId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _unitOfWork.NurseryCareServiceRepository.PrepareCreate(nurseryCareService);
            await _unitOfWork.SaveAsync();
            await InvalidateCacheAsync(nursery.Id);

            var created = await _unitOfWork.NurseryCareServiceRepository.GetByIdWithDetailsAsync(nurseryCareService.Id);
            return MapToDto(created!);
        }

        public async Task<NurseryCareServiceResponseDto> ToggleActiveAsync(int managerId, int id)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

            var item = await _unitOfWork.NurseryCareServiceRepository.GetByIdWithDetailsAsync(id);
            if (item == null)
                throw new NotFoundException($"NurseryCareService {id} not found");

            if (item.NurseryId != nursery.Id)
                throw new ForbiddenException("This service does not belong to your nursery");

            item.IsActive = !item.IsActive;
            _unitOfWork.NurseryCareServiceRepository.PrepareUpdate(item);
            await _unitOfWork.SaveAsync();
            await InvalidateCacheAsync(nursery.Id);

            var updated = await _unitOfWork.NurseryCareServiceRepository.GetByIdWithDetailsAsync(id);
            return MapToDto(updated!);
        }

        public async Task RemoveFromNurseryAsync(int managerId, int id)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

            var item = await _unitOfWork.NurseryCareServiceRepository.GetByIdWithDetailsAsync(id);
            if (item == null)
                throw new NotFoundException($"NurseryCareService {id} not found");

            if (item.NurseryId != nursery.Id)
                throw new ForbiddenException("This service does not belong to your nursery");

            if (item.ServiceRegistrations.Any())
                throw new BadRequestException("Cannot remove a service that has existing registrations");

            await _unitOfWork.NurseryCareServiceRepository.RemoveAsync(item);
            await _unitOfWork.SaveAsync();
            await InvalidateCacheAsync(nursery.Id);
        }

        private async Task InvalidateCacheAsync(int nurseryId)
        {
            await _cacheService.RemoveByPrefixAsync($"{CACHE_KEY_PREFIX}_{nurseryId}_");
        }

        public static NurseryCareServiceResponseDto MapToDto(NurseryCareService ncs)
        {
            return new NurseryCareServiceResponseDto
            {
                Id = ncs.Id,
                NurseryId = ncs.NurseryId,
                NurseryName = ncs.Nursery?.Name,
                IsActive = ncs.IsActive,
                CreatedAt = ncs.CreatedAt,
                CareServicePackage = ncs.CareServicePackage == null ? null
                    : CareServicePackageService.MapToDto(ncs.CareServicePackage)
            };
        }
    }
}
