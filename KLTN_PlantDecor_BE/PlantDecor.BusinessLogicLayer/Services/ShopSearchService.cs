using PlantDecor.BusinessLogicLayer.DTOs.Requests;
using PlantDecor.BusinessLogicLayer.DTOs.Responses;
using PlantDecor.BusinessLogicLayer.Interfaces;
using PlantDecor.DataAccessLayer.Enums;
using PlantDecor.DataAccessLayer.Helpers;
using System.Text;

namespace PlantDecor.BusinessLogicLayer.Services
{
    public class ShopSearchService : IShopSearchService
    {
        private const string SHOP_UNIFIED_SEARCH_KEY = "shop_unified_search_v5";

        private readonly IPlantService _plantService;
        private readonly INurseryMaterialService _nurseryMaterialService;
        private readonly IPlantComboService _plantComboService;
        private readonly ICacheService _cacheService;

        public ShopSearchService(
            IPlantService plantService,
            INurseryMaterialService nurseryMaterialService,
            IPlantComboService plantComboService,
            ICacheService cacheService)
        {
            _plantService = plantService;
            _nurseryMaterialService = nurseryMaterialService;
            _plantComboService = plantComboService;
            _cacheService = cacheService;
        }

        public async Task<ShopUnifiedSearchResponseDto> SearchAsync(ShopUnifiedSearchRequestDto request)
        {
            var searchRequest = request ?? new ShopUnifiedSearchRequestDto();
            var pagination = searchRequest.Pagination ?? new Pagination();
            var cacheKey = BuildCacheKey(searchRequest, pagination);

            var cachedResponse = await _cacheService.GetDataAsync<ShopUnifiedSearchResponseDto>(cacheKey);
            if (cachedResponse != null)
                return cachedResponse;

            // Count enabled categories and distribute pageSize among them
            var enabledCount = (searchRequest.IncludePlants ? 1 : 0)
                             + (searchRequest.IncludeMaterials ? 1 : 0)
                             + (searchRequest.IncludeCombos ? 1 : 0);

            if (enabledCount == 0)
            {
                var emptyResponse = new ShopUnifiedSearchResponseDto
                {
                    Keyword = searchRequest.Keyword,
                    Items = new PaginatedResult<ShopSearchItemDto>(
                        Array.Empty<ShopSearchItemDto>(), 0, pagination.PageNumber, pagination.PageSize)
                };
                return emptyResponse;
            }

            int baseSlots = pagination.PageSize / enabledCount;
            int remainder = pagination.PageSize % enabledCount;

            int AllocateSlots(bool enabled)
            {
                if (!enabled) return 0;
                int slots = baseSlots;
                if (remainder > 0) { slots++; remainder--; }
                return slots;
            }

            int plantPageSize = AllocateSlots(searchRequest.IncludePlants);
            int materialPageSize = AllocateSlots(searchRequest.IncludeMaterials);
            int comboPageSize = AllocateSlots(searchRequest.IncludeCombos);

            // Fetch each category with its allocated page size
            var plantItems = new List<ShopSearchItemDto>();
            var materialItems = new List<ShopSearchItemDto>();
            var comboItems = new List<ShopSearchItemDto>();
            int plantTotal = 0, materialTotal = 0, comboTotal = 0;

            if (searchRequest.IncludePlants && plantPageSize > 0)
            {
                var plantResult = await _plantService.SearchPlantsForShopAsync(new PlantSearchRequestDto
                {
                    Pagination = new Pagination(pagination.PageNumber, plantPageSize),
                    Keyword = searchRequest.Keyword,
                    PlacementType = searchRequest.PlacementType,
                    CareLevelType = searchRequest.CareLevelType,
                    CareLevel = searchRequest.CareLevel,
                    Toxicity = searchRequest.Toxicity,
                    AirPurifying = searchRequest.AirPurifying,
                    HasFlower = searchRequest.HasFlower,
                    PetSafe = searchRequest.PetSafe,
                    ChildSafe = searchRequest.ChildSafe,
                    IsUniqueInstance = searchRequest.IsUniqueInstance,
                    MinBasePrice = searchRequest.MinPrice,
                    MaxBasePrice = searchRequest.MaxPrice,
                    CategoryIds = searchRequest.CategoryIds,
                    TagIds = searchRequest.TagIds,
                    Sizes = searchRequest.Sizes,
                    FengShuiElement = searchRequest.FengShuiElement,
                    NurseryId = searchRequest.NurseryId,
                    SortBy = MapUnifiedToPlantSort(searchRequest.SortBy),
                    SortDirection = searchRequest.SortDirection
                });
                plantTotal = plantResult.TotalCount;
                plantItems.AddRange(plantResult.Items.Select(p => new ShopSearchItemDto
                {
                    Type = "Plant",
                    Plant = p
                }));
            }

            if (searchRequest.IncludeMaterials && materialPageSize > 0)
            {
                var materialPagination = new Pagination(pagination.PageNumber, materialPageSize);
                var materialResult = await _nurseryMaterialService.SearchNurseryMaterialsForShopAsync(
                    new NurseryMaterialShopSearchRequestDto
                    {
                        Pagination = materialPagination,
                        SearchTerm = searchRequest.Keyword,
                        CategoryIds = searchRequest.CategoryIds,
                        TagIds = searchRequest.TagIds,
                        MinPrice = searchRequest.MinPrice.HasValue ? (double?)searchRequest.MinPrice.Value : null,
                        MaxPrice = searchRequest.MaxPrice.HasValue ? (double?)searchRequest.MaxPrice.Value : null,
                        SortBy = MapUnifiedToMaterialSort(searchRequest.SortBy),
                        SortDirection = searchRequest.SortDirection
                    },
                    materialPagination);
                materialTotal = materialResult.TotalCount;
                materialItems.AddRange(materialResult.Items.Select(m => new ShopSearchItemDto
                {
                    Type = "Material",
                    Material = m
                }));
            }

            if (searchRequest.IncludeCombos && comboPageSize > 0)
            {
                var comboPagination = new Pagination(pagination.PageNumber, comboPageSize);
                var comboResult = await _plantComboService.GetSellingCombosAsync(
                    comboPagination,
                    new PlantComboShopSearchRequestDto
                    {
                        Pagination = comboPagination,
                        Keyword = searchRequest.Keyword,
                        MinPrice = searchRequest.MinPrice,
                        MaxPrice = searchRequest.MaxPrice,
                        PetSafe = searchRequest.PetSafe,
                        ChildSafe = searchRequest.ChildSafe,
                        Season = searchRequest.ComboSeason,
                        ComboType = searchRequest.ComboType,
                        TagIds = searchRequest.TagIds,
                        SortBy = MapUnifiedToComboSort(searchRequest.SortBy),
                        SortDirection = searchRequest.SortDirection
                    });
                comboTotal = comboResult.TotalCount;
                comboItems.AddRange(comboResult.Items.Select(c => new ShopSearchItemDto
                {
                    Type = "Combo",
                    Combo = c
                }));
            }

            // Interleave items from different categories for visual variety
            var interleaved = InterleaveItems(plantItems, materialItems, comboItems);
            var grandTotal = plantTotal + materialTotal + comboTotal;

            var response = new ShopUnifiedSearchResponseDto
            {
                Keyword = searchRequest.Keyword,
                Items = new PaginatedResult<ShopSearchItemDto>(
                    interleaved, grandTotal, pagination.PageNumber, pagination.PageSize),
                PlantTotalCount = plantTotal,
                MaterialTotalCount = materialTotal,
                ComboTotalCount = comboTotal
            };

            await _cacheService.SetDataAsync(cacheKey, response, DateTimeOffset.Now.AddMinutes(10));

            return response;
        }

        private static List<ShopSearchItemDto> InterleaveItems(
            List<ShopSearchItemDto> plantItems,
            List<ShopSearchItemDto> materialItems,
            List<ShopSearchItemDto> comboItems)
        {
            var result = new List<ShopSearchItemDto>();
            var lists = new[] { plantItems, materialItems, comboItems }
                .Where(l => l.Count > 0)
                .ToList();

            if (lists.Count == 0) return result;

            int maxLen = lists.Max(l => l.Count);
            for (int i = 0; i < maxLen; i++)
            {
                foreach (var list in lists)
                {
                    if (i < list.Count)
                        result.Add(list[i]);
                }
            }

            return result;
        }

        private static string BuildCacheKey(ShopUnifiedSearchRequestDto request, Pagination pagination)
        {
            var builder = new StringBuilder();
            builder.Append($"{SHOP_UNIFIED_SEARCH_KEY}_p{pagination.PageNumber}_s{pagination.PageSize}");
            builder.Append($"_k{Normalize(request.Keyword)}");
            builder.Append($"_min{request.MinPrice}_max{request.MaxPrice}");
            builder.Append($"_ps{request.PetSafe}_cs{request.ChildSafe}");
            builder.Append($"_cseason{NormalizeNullableInt(request.ComboSeason)}");
            builder.Append($"_ctype{NormalizeNullableInt(request.ComboType)}");
            builder.Append($"_pt{request.PlacementType}_clt{request.CareLevelType}_cl{Normalize(request.CareLevel)}");
            builder.Append($"_tx{request.Toxicity}_ap{request.AirPurifying}_hf{request.HasFlower}_ui{request.IsUniqueInstance}");
            builder.Append($"_sz{NormalizeList(request.Sizes)}_cat{NormalizeList(request.CategoryIds)}_tag{NormalizeList(request.TagIds)}");
            builder.Append($"_fe{NormalizeNullableInt(request.FengShuiElement)}_n{request.NurseryId}");
            builder.Append($"_sb{NormalizeEnum(request.SortBy)}_sd{NormalizeEnum(request.SortDirection)}");
            builder.Append($"_ip{request.IncludePlants}_im{request.IncludeMaterials}_ic{request.IncludeCombos}");
            return builder.ToString();
        }

        private static string Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "none"
                : value.Trim().ToLowerInvariant();
        }

        private static string NormalizeNullableInt(int? value)
        {
            return value.HasValue
                ? value.Value.ToString()
                : "none";
        }

        private static string NormalizeEnum<TEnum>(TEnum? value) where TEnum : struct, Enum
        {
            return value.HasValue
                ? value.Value.ToString().ToLowerInvariant()
                : "none";
        }

        private static string NormalizeList(IEnumerable<int>? values)
        {
            if (values == null)
                return "none";

            var normalized = values
                .Distinct()
                .OrderBy(value => value)
                .ToList();

            return normalized.Count == 0 ? "none" : string.Join("-", normalized);
        }

        private static PlantSortByEnum? MapUnifiedToPlantSort(UnifiedSearchSortByEnum? sortBy)
        {
            return sortBy switch
            {
                UnifiedSearchSortByEnum.CreatedAt => PlantSortByEnum.CreatedAt,
                UnifiedSearchSortByEnum.Name => PlantSortByEnum.Name,
                UnifiedSearchSortByEnum.Price => PlantSortByEnum.Price,
                UnifiedSearchSortByEnum.Size => PlantSortByEnum.Size,
                UnifiedSearchSortByEnum.AvailableInstances => PlantSortByEnum.AvailableInstances,
                _ => null
            };
        }

        private static NurseryMaterialSortByEnum? MapUnifiedToMaterialSort(UnifiedSearchSortByEnum? sortBy)
        {
            return sortBy switch
            {
                UnifiedSearchSortByEnum.Name => NurseryMaterialSortByEnum.Name,
                UnifiedSearchSortByEnum.Price => NurseryMaterialSortByEnum.Price,
                UnifiedSearchSortByEnum.CreatedAt => NurseryMaterialSortByEnum.Newest,
                _ => null
            };
        }

        private static PlantComboSortByEnum? MapUnifiedToComboSort(UnifiedSearchSortByEnum? sortBy)
        {
            return sortBy switch
            {
                UnifiedSearchSortByEnum.Name => PlantComboSortByEnum.Name,
                UnifiedSearchSortByEnum.Price => PlantComboSortByEnum.Price,
                _ => null
            };
        }
    }
}