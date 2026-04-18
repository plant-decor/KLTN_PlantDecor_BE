using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class NurseryDesignTemplateService : INurseryDesignTemplateService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string CACHE_KEY_PREFIX = "nursery_design_tpl";
        private const string CACHE_KEY_PUBLIC_NURSERY_PREFIX = "nursery_design_tpl_public_nursery";
        private const string CACHE_KEY_PUBLIC_TEMPLATE_PREFIX = "nursery_design_tpl_public_template";
        private const string DESIGN_TEMPLATE_CACHE_PREFIX = "design_tpl";

        public NurseryDesignTemplateService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<List<NurseryDesignTemplateResponseDto>> GetActiveByNurseryIdAsync(int nurseryId)
        {
            var cacheKey = $"{CACHE_KEY_PUBLIC_NURSERY_PREFIX}_{nurseryId}";
            var cached = await _cacheService.GetDataAsync<List<NurseryDesignTemplateResponseDto>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var nursery = await _unitOfWork.NurseryRepository.GetByIdAsync(nurseryId)
                ?? throw new NotFoundException($"Nursery {nurseryId} not found");

            var items = await _unitOfWork.NurseryDesignTemplateRepository.GetByNurseryIdAsync(nurseryId, activeOnly: true);
            var result = items.Select(i => MapToDto(i, nursery.Name)).ToList();

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<List<NurseryDesignTemplateResponseDto>> GetActiveByTemplateIdAsync(int designTemplateId)
        {
            var cacheKey = $"{CACHE_KEY_PUBLIC_TEMPLATE_PREFIX}_{designTemplateId}";
            var cached = await _cacheService.GetDataAsync<List<NurseryDesignTemplateResponseDto>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var template = await _unitOfWork.DesignTemplateRepository.GetByIdAsync(designTemplateId)
                ?? throw new NotFoundException($"DesignTemplate {designTemplateId} not found");

            var items = await _unitOfWork.NurseryDesignTemplateRepository.GetByTemplateIdAsync(designTemplateId, activeOnly: true);
            var result = items.Select(i => MapToDto(i, templateName: template.Name)).ToList();

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<List<NurseryDesignTemplateResponseDto>> GetByManagerAsync(int managerId, bool activeOnly = false)
        {
            var nursery = await GetManagedNurseryAsync(managerId);

            var items = await _unitOfWork.NurseryDesignTemplateRepository
                .GetByNurseryIdAsync(nursery.Id, activeOnly: activeOnly);

            return items.Select(i => MapToDto(i, nursery.Name)).ToList();
        }

        public async Task<List<DesignTemplateOptionResponseDto>> GetNotOfferedByManagerAsync(int managerId)
        {
            var nursery = await GetManagedNurseryAsync(managerId);

            var allTemplates = await _unitOfWork.DesignTemplateRepository.GetAllAsync();
            var activeMappings = await _unitOfWork.NurseryDesignTemplateRepository.GetByNurseryIdAsync(nursery.Id, activeOnly: true);
            var offeredTemplateIds = activeMappings.Select(x => x.DesignTemplateId).ToHashSet();

            return allTemplates
                .Where(t => !offeredTemplateIds.Contains(t.Id))
                .OrderBy(t => t.Name)
                .Select(t => new DesignTemplateOptionResponseDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Description = t.Description,
                    ImageUrl = t.ImageUrl
                })
                .ToList();
        }

        public async Task<NurseryDesignTemplateResponseDto> AddToMyNurseryAsync(int managerId, CreateNurseryDesignTemplateRequestDto request)
        {
            var nursery = await GetManagedNurseryAsync(managerId);
            var template = await _unitOfWork.DesignTemplateRepository.GetByIdAsync(request.DesignTemplateId)
                ?? throw new NotFoundException($"DesignTemplate {request.DesignTemplateId} not found");

            var exists = await _unitOfWork.NurseryDesignTemplateRepository
                .ExistsByNurseryAndTemplateAsync(nursery.Id, request.DesignTemplateId);
            if (exists)
            {
                throw new BadRequestException("This template has already been imported to your nursery");
            }

            var entity = new NurseryDesignTemplate
            {
                NurseryId = nursery.Id,
                DesignTemplateId = request.DesignTemplateId,
                IsActive = true,
                CreatedAt = DateTime.Now
            };

            _unitOfWork.NurseryDesignTemplateRepository.PrepareCreate(entity);
            await _unitOfWork.SaveAsync();
            await InvalidateCacheAsync();

            return new NurseryDesignTemplateResponseDto
            {
                Id = entity.Id,
                NurseryId = entity.NurseryId,
                NurseryName = nursery.Name,
                DesignTemplateId = entity.DesignTemplateId,
                DesignTemplateName = template.Name,
                IsActive = entity.IsActive,
                CreatedAt = entity.CreatedAt
            };
        }

        public async Task<NurseryDesignTemplateResponseDto> ToggleActiveAsync(int managerId, int nurseryDesignTemplateId)
        {
            var nursery = await GetManagedNurseryAsync(managerId);

            var mapping = await _unitOfWork.NurseryDesignTemplateRepository.GetByIdAsync(nurseryDesignTemplateId)
                ?? throw new NotFoundException($"NurseryDesignTemplate {nurseryDesignTemplateId} not found");

            if (mapping.NurseryId != nursery.Id)
            {
                throw new ForbiddenException("This template mapping does not belong to your nursery");
            }

            mapping.IsActive = !mapping.IsActive;
            _unitOfWork.NurseryDesignTemplateRepository.PrepareUpdate(mapping);
            await _unitOfWork.SaveAsync();
            await InvalidateCacheAsync();

            var template = await _unitOfWork.DesignTemplateRepository.GetByIdAsync(mapping.DesignTemplateId);

            return new NurseryDesignTemplateResponseDto
            {
                Id = mapping.Id,
                NurseryId = mapping.NurseryId,
                NurseryName = nursery.Name,
                DesignTemplateId = mapping.DesignTemplateId,
                DesignTemplateName = template?.Name,
                IsActive = mapping.IsActive,
                CreatedAt = mapping.CreatedAt
            };
        }

        public async Task RemoveFromMyNurseryAsync(int managerId, int nurseryDesignTemplateId)
        {
            var nursery = await GetManagedNurseryAsync(managerId);

            var mapping = await _unitOfWork.NurseryDesignTemplateRepository.GetByIdAsync(nurseryDesignTemplateId)
                ?? throw new NotFoundException($"NurseryDesignTemplate {nurseryDesignTemplateId} not found");

            if (mapping.NurseryId != nursery.Id)
            {
                throw new ForbiddenException("This template mapping does not belong to your nursery");
            }

            _unitOfWork.NurseryDesignTemplateRepository.PrepareRemove(mapping);
            await _unitOfWork.SaveAsync();
            await InvalidateCacheAsync();
        }

        private async Task InvalidateCacheAsync()
        {
            await _cacheService.RemoveByPrefixAsync(CACHE_KEY_PREFIX);
            await _cacheService.RemoveByPrefixAsync(DESIGN_TEMPLATE_CACHE_PREFIX);
        }

        private async Task<Nursery> GetManagedNurseryAsync(int managerId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
            {
                throw new ForbiddenException("You are not a manager of any nursery");
            }

            return nursery;
        }

        private static NurseryDesignTemplateResponseDto MapToDto(
            NurseryDesignTemplate item,
            string? nurseryName = null,
            string? templateName = null)
        {
            return new NurseryDesignTemplateResponseDto
            {
                Id = item.Id,
                NurseryId = item.NurseryId,
                NurseryName = nurseryName ?? item.Nursery?.Name,
                DesignTemplateId = item.DesignTemplateId,
                DesignTemplateName = templateName ?? item.DesignTemplate?.Name,
                IsActive = item.IsActive,
                CreatedAt = item.CreatedAt
            };
        }
    }
}
