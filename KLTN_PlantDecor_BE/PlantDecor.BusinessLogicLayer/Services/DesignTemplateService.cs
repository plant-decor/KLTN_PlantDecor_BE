using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class DesignTemplateService : IDesignTemplateService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string CACHE_KEY_ALL = "design_tpl_public_all";
        private const string CACHE_KEY_PREFIX = "design_tpl";

        public DesignTemplateService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<List<DesignTemplateResponseDto>> GetAllAsync()
        {
            var cached = await _cacheService.GetDataAsync<List<DesignTemplateResponseDto>>(CACHE_KEY_ALL);
            if (cached != null)
            {
                return cached;
            }

            var templates = await _unitOfWork.DesignTemplateRepository.GetAllAsync();
            var result = new List<DesignTemplateResponseDto>(templates.Count);

            foreach (var template in templates.OrderBy(t => t.Id))
            {
                var detailed = await _unitOfWork.DesignTemplateRepository.GetByIdWithDetailsAsync(template.Id);
                if (detailed != null)
                {
                    result.Add(MapToDto(detailed));
                }
            }

            await _cacheService.SetDataAsync(CACHE_KEY_ALL, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<DesignTemplateResponseDto> GetByIdAsync(int id)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}_{id}";
            var cached = await _cacheService.GetDataAsync<DesignTemplateResponseDto>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var template = await _unitOfWork.DesignTemplateRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignTemplate {id} not found");

            var result = MapToDto(template);
            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<DesignTemplateResponseDto> CreateAsync(CreateDesignTemplateRequestDto request)
        {
            ValidateTemplateName(request.Name);
            await EnsureTemplateNameUniqueAsync(request.Name);

            var entity = new DesignTemplate
            {
                Name = request.Name.Trim(),
                Description = request.Description,
                Style = request.Style,
                RoomTypes = request.RoomTypes,
                ImageUrl = request.ImageUrl,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.DesignTemplateRepository.PrepareCreate(entity);
                await _unitOfWork.SaveAsync();

                if (request.SpecializationIds != null && request.SpecializationIds.Any())
                {
                    await ReplaceSpecializationsInternalAsync(entity.Id, request.SpecializationIds);
                    await _unitOfWork.SaveAsync();
                }

                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var created = await _unitOfWork.DesignTemplateRepository.GetByIdWithDetailsAsync(entity.Id)
                ?? throw new NotFoundException($"DesignTemplate {entity.Id} not found");

            await InvalidateCacheAsync();
            return MapToDto(created);
        }

        public async Task<DesignTemplateResponseDto> UpdateAsync(int id, UpdateDesignTemplateRequestDto request)
        {
            var template = await _unitOfWork.DesignTemplateRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"DesignTemplate {id} not found");

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                ValidateTemplateName(request.Name);
                await EnsureTemplateNameUniqueAsync(request.Name, id);
                template.Name = request.Name.Trim();
            }

            if (request.Description != null)
                template.Description = request.Description;
            if (request.Style.HasValue)
                template.Style = request.Style;
            if (request.RoomTypes != null)
                template.RoomTypes = request.RoomTypes;
            if (request.ImageUrl != null)
                template.ImageUrl = request.ImageUrl;

            template.UpdatedAt = DateTime.Now;

            _unitOfWork.DesignTemplateRepository.PrepareUpdate(template);
            await _unitOfWork.SaveAsync();
            await InvalidateCacheAsync();

            var updated = await _unitOfWork.DesignTemplateRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignTemplate {id} not found");

            return MapToDto(updated);
        }

        public async Task<DesignTemplateResponseDto> UpdateSpecializationsAsync(int templateId, List<int> specializationIds)
        {
            var template = await _unitOfWork.DesignTemplateRepository.GetByIdAsync(templateId)
                ?? throw new NotFoundException($"DesignTemplate {templateId} not found");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await ReplaceSpecializationsInternalAsync(template.Id, specializationIds);
                template.UpdatedAt = DateTime.Now;
                _unitOfWork.DesignTemplateRepository.PrepareUpdate(template);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            await InvalidateCacheAsync();
            var updated = await _unitOfWork.DesignTemplateRepository.GetByIdWithDetailsAsync(templateId)
                ?? throw new NotFoundException($"DesignTemplate {templateId} not found");

            return MapToDto(updated);
        }

        public async Task DeleteAsync(int id)
        {
            var template = await _unitOfWork.DesignTemplateRepository.GetByIdWithDetailsAsync(id)
                ?? throw new NotFoundException($"DesignTemplate {id} not found");

            if (template.DesignTemplateTiers.Any(t => t.DesignRegistrations.Any()))
            {
                throw new BadRequestException("Cannot delete template because there are registrations on its tiers");
            }

            if (template.NurseryDesignTemplates.Any(n => n.IsActive))
            {
                throw new BadRequestException("Cannot delete template while it is actively offered by nurseries");
            }

            var links = await _unitOfWork.DesignTemplateSpecializationRepository.GetByTemplateIdAsync(id);
            foreach (var link in links)
            {
                _unitOfWork.DesignTemplateSpecializationRepository.PrepareRemove(link);
            }

            var trackedTemplate = await _unitOfWork.DesignTemplateRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"DesignTemplate {id} not found");

            _unitOfWork.DesignTemplateRepository.PrepareRemove(trackedTemplate);
            await _unitOfWork.SaveAsync();
            await InvalidateCacheAsync();
        }

        private async Task InvalidateCacheAsync()
        {
            await _cacheService.RemoveByPrefixAsync(CACHE_KEY_PREFIX);
        }

        private async Task ReplaceSpecializationsInternalAsync(int templateId, List<int>? specializationIds)
        {
            var normalizedIds = (specializationIds ?? new List<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            foreach (var specializationId in normalizedIds)
            {
                var specialization = await _unitOfWork.SpecializationRepository.GetByIdAsync(specializationId);
                if (specialization == null)
                {
                    throw new NotFoundException($"Specialization {specializationId} not found");
                }
            }

            var existing = await _unitOfWork.DesignTemplateSpecializationRepository.GetByTemplateIdAsync(templateId);
            foreach (var link in existing)
            {
                _unitOfWork.DesignTemplateSpecializationRepository.PrepareRemove(link);
            }

            foreach (var specializationId in normalizedIds)
            {
                _unitOfWork.DesignTemplateSpecializationRepository.PrepareCreate(new DesignTemplateSpecialization
                {
                    DesignTemplateId = templateId,
                    SpecializationId = specializationId
                });
            }
        }

        private static void ValidateTemplateName(string? name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new BadRequestException("Template name is required");
            }
        }

        private async Task EnsureTemplateNameUniqueAsync(string name, int? excludeId = null)
        {
            var normalized = name.Trim();
            var templates = await _unitOfWork.DesignTemplateRepository.GetAllAsync();
            var exists = templates.Any(t =>
                (excludeId == null || t.Id != excludeId.Value) &&
                string.Equals(t.Name, normalized, StringComparison.OrdinalIgnoreCase));

            if (exists)
            {
                throw new BadRequestException("A design template with the same name already exists");
            }
        }

        public static DesignTemplateResponseDto MapToDto(DesignTemplate template)
        {
            return new DesignTemplateResponseDto
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                Style = template.Style,
                RoomTypes = template.RoomTypes,
                ImageUrl = template.ImageUrl,
                CreatedAt = template.CreatedAt,
                UpdatedAt = template.UpdatedAt,
                Specializations = template.DesignTemplateSpecializations
                    .Where(x => x.Specialization != null)
                    .Select(x => new SpecializationSummaryDto
                    {
                        Id = x.Specialization.Id,
                        Name = x.Specialization.Name,
                        Description = x.Specialization.Description
                    })
                    .OrderBy(x => x.Name)
                    .ToList(),
                Tiers = template.DesignTemplateTiers
                    .Where(t => t.IsActive)
                    .OrderBy(t => t.MinArea)
                    .ThenBy(t => t.Id)
                    .Select(DesignTemplateTierService.MapToDto)
                    .ToList(),
                NurseryOfferings = template.NurseryDesignTemplates
                    .Where(n => n.IsActive)
                    .Select(n => new DesignTemplateNurserySummaryDto
                    {
                        NurseryDesignTemplateId = n.Id,
                        NurseryId = n.NurseryId,
                        NurseryName = n.Nursery?.Name,
                        IsActive = n.IsActive
                    })
                    .OrderBy(n => n.NurseryName)
                    .ThenBy(n => n.NurseryId)
                    .ToList()
            };
        }
    }
}
