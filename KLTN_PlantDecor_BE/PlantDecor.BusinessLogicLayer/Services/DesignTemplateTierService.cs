using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class DesignTemplateTierService : IDesignTemplateTierService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;

        private const string CACHE_KEY_PREFIX = "design_tpl_tier";
        private const string CACHE_KEY_BY_TEMPLATE_PREFIX = "design_tpl_tier_tpl";
        private const string DESIGN_TEMPLATE_CACHE_PREFIX = "design_tpl";

        public DesignTemplateTierService(IUnitOfWork unitOfWork, ICacheService cacheService)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
        }

        public async Task<List<DesignTemplateTierResponseDto>> GetMarketedTiersAsync()
        {
            var templates = await _unitOfWork.DesignTemplateRepository.GetAllAsync();
            var activeTemplates = templates
                .OrderBy(t => t.Id)
                .ToList();

            var result = new List<DesignTemplateTierResponseDto>();

            foreach (var template in activeTemplates)
            {
                var activeMappings = await _unitOfWork.NurseryDesignTemplateRepository
                    .GetByTemplateIdAsync(template.Id, activeOnly: true);

                if (!activeMappings.Any(x => x.Nursery?.IsActive == true))
                {
                    continue;
                }

                var tiers = await _unitOfWork.DesignTemplateTierRepository
                    .GetByTemplateIdWithItemsAsync(template.Id, activeOnly: true);

                result.AddRange(tiers
                    .OrderBy(t => t.MinArea)
                    .ThenBy(t => t.Id)
                    .Select(MapToDto));
            }

            return result;
        }

        public async Task<List<NurseryDesignTemplateResponseDto>> GetActiveNurseriesByTierIdAsync(int designTemplateTierId)
        {
            var tier = await _unitOfWork.DesignTemplateTierRepository.GetByIdAsync(designTemplateTierId)
                ?? throw new NotFoundException($"DesignTemplateTier {designTemplateTierId} not found");

            if (!tier.IsActive)
                throw new BadRequestException("Selected design template tier is inactive");

            var template = await _unitOfWork.DesignTemplateRepository.GetByIdAsync(tier.DesignTemplateId)
                ?? throw new NotFoundException($"DesignTemplate {tier.DesignTemplateId} not found");

            var activeMappings = await _unitOfWork.NurseryDesignTemplateRepository
                .GetByTemplateIdAsync(tier.DesignTemplateId, activeOnly: true);

            return activeMappings
                .Where(x => x.Nursery?.IsActive == true)
                .OrderBy(x => x.NurseryId)
                .Select(x => new NurseryDesignTemplateResponseDto
                {
                    Id = x.Id,
                    NurseryId = x.NurseryId,
                    NurseryName = x.Nursery?.Name,
                    DesignTemplateId = x.DesignTemplateId,
                    DesignTemplateName = template.Name,
                    IsActive = x.IsActive,
                    CreatedAt = x.CreatedAt
                })
                .ToList();
        }

        public async Task<List<DesignTemplateTierResponseDto>> GetByTemplateIdAsync(int designTemplateId, bool includeInactive = false)
        {
            var template = await _unitOfWork.DesignTemplateRepository.GetByIdAsync(designTemplateId);
            if (template == null)
            {
                throw new NotFoundException($"DesignTemplate {designTemplateId} not found");
            }

            var cacheKey = $"{CACHE_KEY_BY_TEMPLATE_PREFIX}_{designTemplateId}_{(includeInactive ? "all" : "active")}";
            var cached = await _cacheService.GetDataAsync<List<DesignTemplateTierResponseDto>>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var tiers = await _unitOfWork.DesignTemplateTierRepository
                .GetByTemplateIdWithItemsAsync(designTemplateId, activeOnly: !includeInactive);

            var result = tiers
                .OrderBy(t => t.MinArea)
                .ThenBy(t => t.Id)
                .Select(MapToDto)
                .ToList();

            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<DesignTemplateTierResponseDto> GetByIdAsync(int id)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}_{id}";
            var cached = await _cacheService.GetDataAsync<DesignTemplateTierResponseDto>(cacheKey);
            if (cached != null)
            {
                return cached;
            }

            var tier = await _unitOfWork.DesignTemplateTierRepository.GetByIdWithItemsAsync(id)
                ?? throw new NotFoundException($"DesignTemplateTier {id} not found");

            var result = MapToDto(tier);
            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<DesignTemplateTierResponseDto> CreateAsync(CreateDesignTemplateTierRequestDto request)
        {
            var template = await _unitOfWork.DesignTemplateRepository.GetByIdAsync(request.DesignTemplateId);
            if (template == null)
            {
                throw new NotFoundException($"DesignTemplate {request.DesignTemplateId} not found");
            }

            ValidateAreaRange(request.MinArea, request.MaxArea);
            ValidateEstimatedDays(request.EstimatedDays);
            ValidateTierName(request.TierName);

            var entity = new DesignTemplateTier
            {
                DesignTemplateId = request.DesignTemplateId,
                TierName = request.TierName.Trim(),
                MinArea = request.MinArea,
                MaxArea = request.MaxArea,
                PackagePrice = request.PackagePrice,
                ScopedOfWork = request.ScopedOfWork.Trim(),
                EstimatedDays = request.EstimatedDays,
                IsActive = request.IsActive,
                CreatedAt = DateTime.Now
            };

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                _unitOfWork.DesignTemplateTierRepository.PrepareCreate(entity);
                await _unitOfWork.SaveAsync();

                if (request.Items != null && request.Items.Any())
                {
                    await ReplaceTierItemsInternalAsync(entity.Id, request.Items);
                    await _unitOfWork.SaveAsync();
                }

                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var created = await _unitOfWork.DesignTemplateTierRepository.GetByIdWithItemsAsync(entity.Id)
                ?? throw new NotFoundException($"DesignTemplateTier {entity.Id} not found");

            await InvalidateCacheAsync();
            return MapToDto(created);
        }

        public async Task<DesignTemplateTierResponseDto> UpdateAsync(int id, UpdateDesignTemplateTierRequestDto request)
        {
            var tier = await _unitOfWork.DesignTemplateTierRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"DesignTemplateTier {id} not found");

            var minArea = request.MinArea ?? tier.MinArea;
            var maxArea = request.MaxArea ?? tier.MaxArea;
            ValidateAreaRange(minArea, maxArea);

            if (request.EstimatedDays.HasValue)
            {
                ValidateEstimatedDays(request.EstimatedDays.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.TierName))
            {
                ValidateTierName(request.TierName);
                tier.TierName = request.TierName.Trim();
            }

            if (request.MinArea.HasValue)
                tier.MinArea = request.MinArea.Value;
            if (request.MaxArea.HasValue)
                tier.MaxArea = request.MaxArea.Value;
            if (request.PackagePrice.HasValue)
                tier.PackagePrice = request.PackagePrice.Value;
            if (!string.IsNullOrWhiteSpace(request.ScopedOfWork))
                tier.ScopedOfWork = request.ScopedOfWork.Trim();
            if (request.EstimatedDays.HasValue)
                tier.EstimatedDays = request.EstimatedDays.Value;
            if (request.IsActive.HasValue)
                tier.IsActive = request.IsActive.Value;

            _unitOfWork.DesignTemplateTierRepository.PrepareUpdate(tier);
            await _unitOfWork.SaveAsync();
            await InvalidateCacheAsync();

            var updated = await _unitOfWork.DesignTemplateTierRepository.GetByIdWithItemsAsync(id)
                ?? throw new NotFoundException($"DesignTemplateTier {id} not found");

            return MapToDto(updated);
        }

        public async Task<DesignTemplateTierResponseDto> SetItemsAsync(int id, List<DesignTemplateTierItemInputDto> items)
        {
            var tier = await _unitOfWork.DesignTemplateTierRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"DesignTemplateTier {id} not found");

            await _unitOfWork.BeginTransactionAsync();
            try
            {
                await ReplaceTierItemsInternalAsync(tier.Id, items);
                await _unitOfWork.SaveAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync();
                throw;
            }

            var updated = await _unitOfWork.DesignTemplateTierRepository.GetByIdWithItemsAsync(id)
                ?? throw new NotFoundException($"DesignTemplateTier {id} not found");

            await InvalidateCacheAsync();
            return MapToDto(updated);
        }

        public async Task DeleteAsync(int id)
        {
            var tier = await _unitOfWork.DesignTemplateTierRepository.GetByIdWithItemsAsync(id)
                ?? throw new NotFoundException($"DesignTemplateTier {id} not found");

            if (tier.DesignRegistrations.Any())
            {
                throw new BadRequestException("Cannot deactivate tier because it already has design registrations");
            }

            var trackedTier = await _unitOfWork.DesignTemplateTierRepository.GetByIdAsync(id)
                ?? throw new NotFoundException($"DesignTemplateTier {id} not found");

            trackedTier.IsActive = false;
            _unitOfWork.DesignTemplateTierRepository.PrepareUpdate(trackedTier);
            await _unitOfWork.SaveAsync();
            await InvalidateCacheAsync();
        }

        private async Task InvalidateCacheAsync()
        {
            await _cacheService.RemoveByPrefixAsync(CACHE_KEY_PREFIX);
            await _cacheService.RemoveByPrefixAsync(DESIGN_TEMPLATE_CACHE_PREFIX);
        }

        private async Task ReplaceTierItemsInternalAsync(int tierId, List<DesignTemplateTierItemInputDto> items)
        {
            var normalizedItems = items ?? new List<DesignTemplateTierItemInputDto>();

            foreach (var item in normalizedItems)
            {
                ValidateTierItem(item);

                if (item.MaterialId.HasValue)
                {
                    var material = await _unitOfWork.MaterialRepository.GetByIdAsync(item.MaterialId.Value);
                    if (material == null)
                        throw new NotFoundException($"Material {item.MaterialId.Value} not found");
                }

                if (item.PlantId.HasValue)
                {
                    var plant = await _unitOfWork.PlantRepository.GetByIdAsync(item.PlantId.Value);
                    if (plant == null)
                        throw new NotFoundException($"Plant {item.PlantId.Value} not found");
                }
            }

            var currentItems = await _unitOfWork.DesignTemplateTierItemRepository.GetByTierIdAsync(tierId);
            foreach (var current in currentItems)
            {
                _unitOfWork.DesignTemplateTierItemRepository.PrepareRemove(current);
            }

            foreach (var item in normalizedItems)
            {
                _unitOfWork.DesignTemplateTierItemRepository.PrepareCreate(new DesignTemplateTierItem
                {
                    DesignTemplateTierId = tierId,
                    MaterialId = item.MaterialId,
                    PlantId = item.PlantId,
                    ItemType = item.ItemType,
                    Quantity = item.Quantity,
                    CreatedAt = DateTime.Now
                });
            }
        }

        private static void ValidateAreaRange(decimal minArea, decimal maxArea)
        {
            if (minArea < 0 || maxArea < 0)
                throw new BadRequestException("Area values must be non-negative");
            if (minArea > maxArea)
                throw new BadRequestException("MinArea cannot be greater than MaxArea");
        }

        private static void ValidateEstimatedDays(int estimatedDays)
        {
            if (estimatedDays <= 0)
            {
                throw new BadRequestException("EstimatedDays must be greater than 0");
            }
        }

        private static void ValidateTierName(string? tierName)
        {
            if (string.IsNullOrWhiteSpace(tierName))
            {
                throw new BadRequestException("TierName is required");
            }
        }

        private static void ValidateTierItem(DesignTemplateTierItemInputDto item)
        {
            if (item.ItemType <= 0)
            {
                throw new BadRequestException("ItemType must be greater than 0");
            }

            if (item.Quantity <= 0)
            {
                throw new BadRequestException("Quantity must be greater than 0");
            }

            var hasMaterial = item.MaterialId.HasValue;
            var hasPlant = item.PlantId.HasValue;

            if (hasMaterial == hasPlant)
            {
                throw new BadRequestException("Each tier item must reference either MaterialId or PlantId");
            }
        }

        public static DesignTemplateTierResponseDto MapToDto(DesignTemplateTier tier)
        {
            return new DesignTemplateTierResponseDto
            {
                Id = tier.Id,
                DesignTemplateId = tier.DesignTemplateId,
                TierName = tier.TierName,
                MinArea = tier.MinArea,
                MaxArea = tier.MaxArea,
                PackagePrice = tier.PackagePrice,
                ScopedOfWork = tier.ScopedOfWork,
                EstimatedDays = tier.EstimatedDays,
                IsActive = tier.IsActive,
                CreatedAt = tier.CreatedAt,
                Items = tier.DesignTemplateTierItems
                    .OrderBy(i => i.Id)
                    .Select(i => new DesignTemplateTierItemResponseDto
                    {
                        Id = i.Id,
                        DesignTemplateTierId = i.DesignTemplateTierId,
                        MaterialId = i.MaterialId,
                        PlantId = i.PlantId,
                        ItemType = i.ItemType,
                        Quantity = i.Quantity,
                        CreatedAt = i.CreatedAt
                    })
                    .ToList()
            };
        }
    }
}
