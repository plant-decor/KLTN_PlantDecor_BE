using Hangfire;
using PlantDecor.BusinessLogicLayer.Constants;
using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Exceptions;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.BusinessLogicLayer.Mappings;
using PlantDecor.DataAccessLayer.Entities;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.UnitOfWork;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class CareServicePackageService : ICareServicePackageService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICacheService _cacheService;
        private readonly IBackgroundJobClient _backgroundJobClient;

        private const string CACHE_KEY_ACTIVE = "care_pkg_active";
        private const string CACHE_KEY_ALL = "care_pkg_all";
        private const string CACHE_KEY_PREFIX = "care_pkg";

        public CareServicePackageService(
            IUnitOfWork unitOfWork,
            ICacheService cacheService,
            IBackgroundJobClient backgroundJobClient)
        {
            _unitOfWork = unitOfWork;
            _cacheService = cacheService;
            _backgroundJobClient = backgroundJobClient;
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

        public async Task<CareServicePackageWithNurseriesResponseDto> GetByIdWithNurseriesAsync(int id)
        {
            var cacheKey = $"{CACHE_KEY_PREFIX}_{id}_with_nurseries";
            var cached = await _cacheService.GetDataAsync<CareServicePackageWithNurseriesResponseDto>(cacheKey);
            if (cached != null) return cached;

            var pkg = await _unitOfWork.CareServicePackageRepository.GetByIdWithNurseriesAsync(id);
            if (pkg == null)
                throw new NotFoundException($"CareServicePackage {id} not found");

            var result = MapToWithNurseriesDto(pkg);
            await _cacheService.SetDataAsync(cacheKey, result, DateTimeOffset.Now.AddMinutes(30));
            return result;
        }

        public async Task<CareServicePackageResponseDto> CreateAsync(CreateCareServicePackageRequestDto request)
        {
            if (await _unitOfWork.CareServicePackageRepository.ExistsByNameAsync(request.Name))
                throw new BadRequestException($"A package named '{request.Name}' already exists");

            if (request.SuitabilityRules == null || request.SuitabilityRules.Count == 0)
                throw new BadRequestException("SuitabilityRules is required when creating a care service package");

            
            if (request.ServiceType == (int)CareServiceTypeEnum.OneTime)
            {
                request.VisitPerWeek = null;
                request.DurationDays = 1;
            }
            else if (request.ServiceType == (int)CareServiceTypeEnum.Periodic &&
                (!request.VisitPerWeek.HasValue || request.VisitPerWeek.Value == 0))
            {
                throw new BadRequestException("VisitPerWeek (1-6) is required for Periodic service packages");
            }
            else if (request.ServiceType == (int)CareServiceTypeEnum.Periodic &&
                (request.VisitPerWeek is < 1 or > 6))
            {
                throw new BadRequestException("VisitPerWeek must be between 1 and 6");
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

            var normalizedSuitabilityRules = await ValidateAndBuildSuitabilityRulesAsync(request.SuitabilityRules, pkg.Id);
            await _unitOfWork.CareServicePackageRepository.AddSuitabilityRulesAsync(pkg.Id, normalizedSuitabilityRules);

            await InvalidateCacheAsync();

            var created = await _unitOfWork.CareServicePackageRepository.GetByIdWithDetailsAsync(pkg.Id);
            QueueEmbeddingAsync(created!);
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
            QueueEmbeddingAsync(updated!);
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

            var updated = await _unitOfWork.CareServicePackageRepository.GetByIdWithDetailsAsync(id);
            if (updated != null)
            {
                QueueEmbeddingAsync(updated);
            }
        }

        private void QueueEmbeddingAsync(CareServicePackage entity)
        {
            try
            {
                var embeddingDto = entity.ToEmbeddingBackfillDto();
                var entityId = ConvertToGuid(entity.Id);
                _backgroundJobClient.Enqueue<IEmbeddingBackgroundJobService>(
                    service => service.ProcessCareServicePackageEmbeddingAsync(
                        embeddingDto,
                        entityId,
                        EmbeddingEntityTypes.CareServicePackage));
            }
            catch
            {
                // Do not fail the main operation if embedding enqueue fails.
            }
        }

        private static Guid ConvertToGuid(int id)
            => new Guid(id.ToString().PadLeft(32, '0'));

        public async Task<List<CareServicePackageWithNurseriesResponseDto>> GetPackagesWithNurseriesAsync()
        {
            var packages = await _unitOfWork.CareServicePackageRepository.GetPackagesWithNurseriesAsync();
            return packages.Select(MapToWithNurseriesDto).ToList();
        }

        public async Task<List<CareServicePackageResponseDto>> GetNotOfferedByManagerAsync(int managerId)
        {
            var nursery = await _unitOfWork.NurseryRepository.GetByManagerIdAsync(managerId);
            if (nursery == null)
                throw new ForbiddenException("You are not a manager of any nursery");

            var packages = await _unitOfWork.CareServicePackageRepository.GetNotActivelyOfferedByNurseryAsync(nursery.Id);
            return packages.Select(MapToDto).ToList();
        }

        public async Task<List<CareServicePackageRecommendationResponseDto>> RecommendByOrderAsync(int consultantId, int orderId)
        {
            await EnsureConsultantPermissionAsync(consultantId);

            var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId);
            if (order == null)
                throw new NotFoundException($"Order {orderId} not found");

            return await RecommendByOrderInternalAsync(order);
        }

        public async Task<List<CareServicePackageRecommendationResponseDto>> RecommendByOrderForCustomerAsync(int userId, int orderId)
        {
            if (userId <= 0)
                throw new UnauthorizedException("Unable to identify user from token");

            var order = await _unitOfWork.OrderRepository.GetByIdWithDetailsAsync(orderId);
            if (order == null)
                throw new NotFoundException($"Order {orderId} not found");

            if (order.UserId != userId)
                throw new ForbiddenException("You do not have permission to access this order");

            return await RecommendByOrderInternalAsync(order);
        }

        private async Task<List<CareServicePackageRecommendationResponseDto>> RecommendByOrderInternalAsync(Order order)
        {
            var offeredPackages = await _unitOfWork.CareServicePackageRepository.GetPackagesWithNurseriesAsync();
            if (offeredPackages.Count == 0)
                return new List<CareServicePackageRecommendationResponseDto>();

            var profile = BuildOrderPlantProfile(order);
            if (profile.TotalPlantItems == 0)
                return new List<CareServicePackageRecommendationResponseDto>();

            var packageIds = offeredPackages.Select(p => p.Id).ToList();
            var rules = await _unitOfWork.CareServicePackageRepository.GetActiveSuitabilityRulesByPackageIdsAsync(packageIds);
            var rulesByPackageId = rules
                .GroupBy(r => r.CareServicePackageId)
                .ToDictionary(g => g.Key, g => g.ToList());

            return BuildRecommendations(profile, offeredPackages, rulesByPackageId);
        }

        private static List<CareServicePackageRecommendationResponseDto> BuildRecommendations(
            CustomerPlantProfile profile,
            List<CareServicePackage> offeredPackages,
            Dictionary<int, List<PackagePlantSuitability>> rulesByPackageId)
        {
            // Step 1: Assign each purchased plant to the best matching package.
            var recommendationsByPackageId = new Dictionary<int, CareServicePackageRecommendationResponseDto>();
            var packageMatchedCategories = new Dictionary<int, Dictionary<string, int>>();
            var packageMatchedCareLevels = new Dictionary<int, Dictionary<int, int>>();

            foreach (var plant in profile.PurchasedPlants)
            {
                CareServicePackage? bestPackage = null;
                int bestCategoryMatch = -1;
                int bestCareMatch = -1;
                var bestMatchedCategoryNames = new List<string>();
                var bestMatchedCareLevels = new List<int>();

                foreach (var package in offeredPackages)
                {
                    if (!rulesByPackageId.TryGetValue(package.Id, out var packageRules) || packageRules.Count == 0)
                        continue;

                    var currentMatchedCategoryNames = packageRules
                        .Where(r => r.CategoryId.HasValue && plant.CategoryIds.Contains(r.CategoryId.Value))
                        .Select(r => string.IsNullOrWhiteSpace(r.Category?.Name) ? $"Category {r.CategoryId!.Value}" : r.Category!.Name!)
                        .Distinct()
                        .ToList();

                    var currentMatchedCareLevels = packageRules
                        .Where(r => r.CareDifficultyLevel.HasValue && plant.CareLevelType == r.CareDifficultyLevel.Value)
                        .Select(r => r.CareDifficultyLevel!.Value)
                        .Distinct()
                        .ToList();

                    var categoryMatch = currentMatchedCategoryNames.Count;
                    var careMatch = currentMatchedCareLevels.Count;

                    if (categoryMatch == 0 && careMatch == 0)
                        continue;

                    var isBetterCandidate =
                        bestPackage == null
                        || (categoryMatch > 0 && bestCategoryMatch == 0)
                        || (categoryMatch > bestCategoryMatch)
                        || (categoryMatch == bestCategoryMatch && careMatch > bestCareMatch)
                        || (categoryMatch == bestCategoryMatch
                            && careMatch == bestCareMatch
                            && (package.UnitPrice ?? decimal.MaxValue) < (bestPackage.UnitPrice ?? decimal.MaxValue));

                    if (!isBetterCandidate)
                        continue;

                    bestPackage = package;
                    bestCategoryMatch = categoryMatch;
                    bestCareMatch = careMatch;
                    bestMatchedCategoryNames = currentMatchedCategoryNames;
                    bestMatchedCareLevels = currentMatchedCareLevels;
                }

                if (bestPackage == null)
                    throw new NotFoundException($"No suitable package found for plant '{plant.PlantName}' (PlantId: {plant.PlantId}). Please verify package suitability mapping data.");

                if (!recommendationsByPackageId.TryGetValue(bestPackage.Id, out var recDto))
                {
                    recDto = new CareServicePackageRecommendationResponseDto
                    {
                        PackageId = bestPackage.Id,
                        PackageName = bestPackage.Name ?? string.Empty,
                        UnitPrice = bestPackage.UnitPrice,
                        MatchScore = 0,
                        TotalPurchasedPlantItems = profile.TotalPlantItems,
                        MatchReasons = new List<string>(),
                        Plants = new List<RecommendedPlantDto>()
                    };

                    recommendationsByPackageId[bestPackage.Id] = recDto;
                    packageMatchedCategories[bestPackage.Id] = new Dictionary<string, int>();
                    packageMatchedCareLevels[bestPackage.Id] = new Dictionary<int, int>();
                }

                recDto.Plants.Add(new RecommendedPlantDto
                {
                    PlantId = plant.PlantId,
                    PlantName = plant.PlantName,
                    Quantity = plant.Quantity
                });

                recDto.MatchScore += ((bestCategoryMatch * 2) + bestCareMatch) * plant.Quantity;

                foreach (var categoryName in bestMatchedCategoryNames)
                {
                    if (packageMatchedCategories[bestPackage.Id].ContainsKey(categoryName))
                        packageMatchedCategories[bestPackage.Id][categoryName] += plant.Quantity;
                    else
                        packageMatchedCategories[bestPackage.Id][categoryName] = plant.Quantity;
                }

                foreach (var careLevel in bestMatchedCareLevels)
                {
                    if (packageMatchedCareLevels[bestPackage.Id].ContainsKey(careLevel))
                        packageMatchedCareLevels[bestPackage.Id][careLevel] += plant.Quantity;
                    else
                        packageMatchedCareLevels[bestPackage.Id][careLevel] = plant.Quantity;
                }
            }

            // Step 2: Build reasons and final ranking for only packages that actually received plants.
            var recommendations = recommendationsByPackageId.Values
                .OrderByDescending(r => r.MatchScore)
                .ThenBy(r => r.UnitPrice ?? decimal.MaxValue)
                .ToList();

            foreach (var recDto in recommendations)
            {
                var categories = packageMatchedCategories[recDto.PackageId];
                if (categories.Count > 0)
                {
                    var catReason = string.Join(", ", categories.OrderByDescending(c => c.Value).Select(c => $"{c.Key} ({c.Value})"));
                    recDto.MatchReasons.Add($"Matched categories: {catReason}");
                    recDto.MatchedCategoryCount = categories.Count;
                }

                var careLevels = packageMatchedCareLevels[recDto.PackageId];
                if (careLevels.Count > 0)
                {
                    var lvlReason = string.Join(", ", careLevels.OrderByDescending(c => c.Value).Select(c => $"{MapCareLevelName(c.Key)} ({c.Value})"));
                    recDto.MatchReasons.Add($"Matched care levels: {lvlReason}");
                    recDto.MatchedCareLevelCount = careLevels.Count;
                }
            }

            return recommendations;
        }

        public async Task<CareServicePackageResponseDto> UpdateSpecializationsAsync(int packageId, List<int> specializationIds)
        {
            var pkg = await _unitOfWork.CareServicePackageRepository.GetByIdWithDetailsAsync(packageId);
            if (pkg == null)
                throw new NotFoundException($"CareServicePackage {packageId} not found");

            // Validate that all provided specialization IDs exist
            foreach (var specId in specializationIds.Distinct())
            {
                var spec = await _unitOfWork.SpecializationRepository.GetByIdAsync(specId);
                if (spec == null)
                    throw new NotFoundException($"Specialization {specId} not found");
            }

            await _unitOfWork.CareServicePackageRepository.ReplaceSpecializationsAsync(packageId, specializationIds);
            await InvalidateCacheAsync();

            var updated = await _unitOfWork.CareServicePackageRepository.GetByIdWithDetailsAsync(packageId);
            return MapToDto(updated!);
        }

        public async Task<CareServicePackageResponseDto> UpdateSuitabilityRulesAsync(int packageId, List<PackagePlantSuitabilityRuleRequestDto> rules)
        {
            var pkg = await _unitOfWork.CareServicePackageRepository.GetByIdWithDetailsAsync(packageId);
            if (pkg == null)
                throw new NotFoundException($"CareServicePackage {packageId} not found");

            var normalizedSuitabilityRules = await ValidateAndBuildSuitabilityRulesAsync(rules, packageId);
            await _unitOfWork.CareServicePackageRepository.ReplaceSuitabilityRulesAsync(packageId, normalizedSuitabilityRules);
            await InvalidateCacheAsync();

            var updated = await _unitOfWork.CareServicePackageRepository.GetByIdWithDetailsAsync(packageId);
            QueueEmbeddingAsync(updated!);
            return MapToDto(updated!);
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
                    }).ToList(),
                SuitabilityRules = pkg.PackagePlantSuitabilities
                    .Where(pps => pps.IsActive)
                    .Select(pps => new PackagePlantSuitabilityRuleResponseDto
                    {
                        Id = pps.Id,
                        CareServicePackageId = pps.CareServicePackageId,
                        CategoryId = pps.CategoryId,
                        CategoryName = pps.Category?.Name,
                        CareDifficultyLevel = pps.CareDifficultyLevel,
                        CareDifficultyLevelName = pps.CareDifficultyLevel.HasValue ? MapCareLevelName(pps.CareDifficultyLevel.Value) : null
                    }).ToList()
            };
        }

        public static CareServicePackageWithNurseriesResponseDto MapToWithNurseriesDto(CareServicePackage pkg)
        {
            var baseDto = MapToDto(pkg);

            return new CareServicePackageWithNurseriesResponseDto
            {
                Id = baseDto.Id,
                Name = baseDto.Name,
                Description = baseDto.Description,
                Features = baseDto.Features,
                VisitPerWeek = baseDto.VisitPerWeek,
                DurationDays = baseDto.DurationDays,
                TotalSessions = baseDto.TotalSessions,
                ServiceType = baseDto.ServiceType,
                AreaLimit = baseDto.AreaLimit,
                UnitPrice = baseDto.UnitPrice,
                IsActive = baseDto.IsActive,
                CreatedAt = baseDto.CreatedAt,
                Specializations = baseDto.Specializations,
                NurseryCareServices = pkg.NurseryCareServices
                    .Where(ncs => ncs.IsActive && ncs.Nursery.IsActive == true)
                    .OrderBy(ncs => ncs.NurseryId)
                    .Select(ncs => new NurseryCareServiceOptionResponseDto
                    {
                        NurseryCareServiceId = ncs.Id,
                        NurseryId = ncs.NurseryId,
                        NurseryName = ncs.Nursery.Name,
                        NurseryAddress = ncs.Nursery.Address,
                        NurseryPhone = ncs.Nursery.Phone
                    })
                    .ToList()
            };
        }

        private async Task EnsureConsultantPermissionAsync(int consultantId)
        {
            var consultant = await _unitOfWork.UserRepository.GetByIdAsync(consultantId);
            if (consultant == null)
                throw new UnauthorizedException("Consultant not found");

            if (consultant.RoleId != (int)RoleEnum.Consultant)
                throw new ForbiddenException("Only consultant can use this recommendation API");
        }

        private async Task<List<PackagePlantSuitability>> ValidateAndBuildSuitabilityRulesAsync(
            IEnumerable<PackagePlantSuitabilityRuleRequestDto> requestRules,
            int packageId)
        {
            var rules = requestRules?.ToList() ?? new List<PackagePlantSuitabilityRuleRequestDto>();
            if (rules.Count == 0)
                throw new BadRequestException("At least one suitability rule is required");

            var uniqueKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var validated = new List<PackagePlantSuitability>();

            foreach (var rule in rules)
            {
                var hasCategory = rule.CategoryId.HasValue;
                var hasCareLevel = rule.CareDifficultyLevel.HasValue;
                if (!hasCategory && !hasCareLevel)
                    throw new BadRequestException("Each suitability rule must have CategoryId or CareDifficultyLevel");
                if (hasCategory && hasCareLevel)
                    throw new BadRequestException("Each suitability rule must contain only one condition");

                if (hasCategory)
                {
                    var category = await _unitOfWork.CategoryRepository.GetByIdAsync(rule.CategoryId!.Value);
                    if (category == null)
                        throw new NotFoundException($"Category {rule.CategoryId.Value} not found");
                }

                if (hasCareLevel && !Enum.IsDefined(typeof(CareLevelTypeEnum), rule.CareDifficultyLevel!.Value))
                    throw new BadRequestException($"CareDifficultyLevel {rule.CareDifficultyLevel.Value} is invalid");

                var key = $"{rule.CategoryId?.ToString() ?? "null"}:{rule.CareDifficultyLevel?.ToString() ?? "null"}";
                if (!uniqueKeys.Add(key))
                    throw new BadRequestException("Duplicate suitability rules are not allowed");

                validated.Add(new PackagePlantSuitability
                {
                    CareServicePackageId = packageId,
                    CategoryId = rule.CategoryId,
                    CareDifficultyLevel = rule.CareDifficultyLevel,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                });
            }

            return validated;
        }

        private static CustomerPlantProfile BuildOrderPlantProfile(Order order)
        {
            return BuildCustomerPlantProfile(new List<Order> { order });
        }

        private static CustomerPlantProfile BuildCustomerPlantProfile(List<Order> orders)
        {
            var categoryPurchaseCounts = new Dictionary<int, int>();
            var careLevelPurchaseCounts = new Dictionary<int, int>();
            var purchasedPlants = new List<PurchasedPlant>();
            var totalPlantItems = 0;

            foreach (var order in orders)
            {
                foreach (var nurseryOrder in order.NurseryOrders)
                {
                    foreach (var detail in nurseryOrder.NurseryOrderDetails)
                    {
                        var lineQuantity = Math.Max(1, detail.Quantity ?? 1);

                        if (detail.CommonPlant?.Plant != null)
                            AddPlantToProfile(detail.CommonPlant.Plant, lineQuantity, categoryPurchaseCounts, careLevelPurchaseCounts, purchasedPlants, ref totalPlantItems);

                        if (detail.PlantInstance?.Plant != null)
                            AddPlantToProfile(detail.PlantInstance.Plant, lineQuantity, categoryPurchaseCounts, careLevelPurchaseCounts, purchasedPlants, ref totalPlantItems);

                        var comboItems = detail.NurseryPlantCombo?.PlantCombo?.PlantComboItems;
                        if (comboItems == null)
                            continue;

                        foreach (var comboItem in comboItems)
                        {
                            if (comboItem.Plant == null)
                                continue;

                            var comboPlantQty = Math.Max(1, comboItem.Quantity ?? 1) * lineQuantity;
                            AddPlantToProfile(comboItem.Plant, comboPlantQty, categoryPurchaseCounts, careLevelPurchaseCounts, purchasedPlants, ref totalPlantItems);
                        }
                    }
                }
            }

            return new CustomerPlantProfile(categoryPurchaseCounts, careLevelPurchaseCounts, totalPlantItems, purchasedPlants);
        }

        private static void AddPlantToProfile(
            Plant plant,
            int quantity,
            Dictionary<int, int> categoryPurchaseCounts,
            Dictionary<int, int> careLevelPurchaseCounts,
            List<PurchasedPlant> purchasedPlants,
            ref int totalPlantItems)
        {
            if (quantity <= 0)
                return;

            totalPlantItems += quantity;

            var existingPlant = purchasedPlants.FirstOrDefault(p => p.PlantId == plant.Id);
            if (existingPlant != null)
            {
                existingPlant.Quantity += quantity;
            }
            else
            {
                purchasedPlants.Add(new PurchasedPlant
                {
                    PlantId = plant.Id,
                    PlantName = plant.Name ?? $"Plant #{plant.Id}",
                    Quantity = quantity,
                    CareLevelType = plant.CareLevelType,
                    CategoryIds = plant.Categories.Select(c => c.Id).ToList()
                });
            }

            if (plant.CareLevelType.HasValue)
            {
                if (careLevelPurchaseCounts.ContainsKey(plant.CareLevelType.Value))
                    careLevelPurchaseCounts[plant.CareLevelType.Value] += quantity;
                else
                    careLevelPurchaseCounts[plant.CareLevelType.Value] = quantity;
            }

            foreach (var category in plant.Categories)
            {
                if (categoryPurchaseCounts.ContainsKey(category.Id))
                    categoryPurchaseCounts[category.Id] += quantity;
                else
                    categoryPurchaseCounts[category.Id] = quantity;
            }
        }

        private static string MapCareLevelName(int level)
        {
            return level switch
            {
                1 => "Easy",
                2 => "Medium",
                3 => "Hard",
                4 => "Expert",
                _ => $"Level {level}"
            };
        }

        private sealed class CustomerPlantProfile
        {
            public CustomerPlantProfile(
                Dictionary<int, int> categoryPurchaseCounts,
                Dictionary<int, int> careLevelPurchaseCounts,
                int totalPlantItems,
                List<PurchasedPlant> purchasedPlants)
            {
                CategoryPurchaseCounts = categoryPurchaseCounts;
                CareLevelPurchaseCounts = careLevelPurchaseCounts;
                TotalPlantItems = totalPlantItems;
                PurchasedPlants = purchasedPlants;
            }

            public Dictionary<int, int> CategoryPurchaseCounts { get; }
            public Dictionary<int, int> CareLevelPurchaseCounts { get; }
            public int TotalPlantItems { get; }
            public List<PurchasedPlant> PurchasedPlants { get; }
        }

        private sealed class PurchasedPlant
        {
            public int PlantId { get; set; }
            public string PlantName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public int? CareLevelType { get; set; }
            public List<int> CategoryIds { get; set; } = new();
        }
    }
}
