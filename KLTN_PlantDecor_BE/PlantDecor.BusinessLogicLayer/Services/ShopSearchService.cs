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
        private const string SHOP_UNIFIED_SEARCH_KEY = "shop_unified_search_v8";

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

            var hasAnySourceEnabled = searchRequest.IncludePlants
                                      || searchRequest.IncludeMaterials
                                      || searchRequest.IncludeCombos;

            if (!hasAnySourceEnabled)
            {
                var emptyResponse = new ShopUnifiedSearchResponseDto
                {
                    Keyword = searchRequest.Keyword,
                    Items = new PaginatedResult<ShopSearchItemDto>(
                        Array.Empty<ShopSearchItemDto>(), 0, pagination.PageNumber, pagination.PageSize)
                };
                return emptyResponse;
            }

            var globalEndIndex = pagination.PageNumber * pagination.PageSize;
            var globalStartIndex = (pagination.PageNumber - 1) * pagination.PageSize;

            var (plantItems, plantTotal) = await FetchPlantWindowItemsAsync(searchRequest, globalEndIndex);
            var (materialItems, materialTotal) = await FetchMaterialWindowItemsAsync(searchRequest, globalEndIndex);
            var (comboItems, comboTotal) = await FetchComboWindowItemsAsync(searchRequest, globalEndIndex);

            var interleaved = InterleaveItems(plantItems, materialItems, comboItems);
            var pagedItems = interleaved
                .Skip(globalStartIndex)
                .Take(pagination.PageSize)
                .ToList();

            var grandTotal = plantTotal + materialTotal + comboTotal;

            var response = new ShopUnifiedSearchResponseDto
            {
                Keyword = searchRequest.Keyword,
                Items = new PaginatedResult<ShopSearchItemDto>(
                    pagedItems, grandTotal, pagination.PageNumber, pagination.PageSize),
                PlantTotalCount = plantTotal,
                MaterialTotalCount = materialTotal,
                ComboTotalCount = comboTotal
            };

            await _cacheService.SetDataAsync(cacheKey, response, DateTimeOffset.Now.AddMinutes(10));

            return response;
        }

        private async Task<(List<ShopSearchItemDto> Items, int TotalCount)> FetchPlantWindowItemsAsync(
            ShopUnifiedSearchRequestDto searchRequest,
            int windowSize)
        {
            if (!searchRequest.IncludePlants || windowSize <= 0)
                return (new List<ShopSearchItemDto>(), 0);

            const int batchSize = 100;
            var collected = new List<ShopSearchItemDto>();
            var pageNumber = 1;
            var totalCount = 0;

            while (collected.Count < windowSize)
            {
                var batchResult = await _plantService.SearchPlantsForShopAsync(new PlantSearchRequestDto
                {
                    Pagination = new Pagination(pageNumber, batchSize),
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

                totalCount = batchResult.TotalCount;
                var batchItems = batchResult.Items
                    .Select(p => new ShopSearchItemDto
                    {
                        Type = "Plant",
                        Plant = p
                    })
                    .ToList();

                if (batchItems.Count == 0)
                    break;

                collected.AddRange(batchItems);

                if (collected.Count >= totalCount)
                    break;

                pageNumber++;
            }

            return (collected.Take(windowSize).ToList(), totalCount);
        }

        private async Task<(List<ShopSearchItemDto> Items, int TotalCount)> FetchMaterialWindowItemsAsync(
            ShopUnifiedSearchRequestDto searchRequest,
            int windowSize)
        {
            if (!searchRequest.IncludeMaterials || windowSize <= 0)
                return (new List<ShopSearchItemDto>(), 0);

            const int batchSize = 100;
            var collected = new List<ShopSearchItemDto>();
            var seenMaterialIds = new HashSet<int>();
            var pageNumber = 1;

            while (true)
            {
                var materialPagination = new Pagination(pageNumber, batchSize);
                var batchResult = await _nurseryMaterialService.SearchNurseryMaterialsForShopAsync(
                    new NurseryMaterialShopSearchRequestDto
                    {
                        Pagination = materialPagination,
                        NurseryId = searchRequest.NurseryId,
                        SearchTerm = searchRequest.Keyword,
                        CategoryIds = searchRequest.CategoryIds,
                        TagIds = searchRequest.TagIds,
                        MinPrice = searchRequest.MinPrice.HasValue ? (double?)searchRequest.MinPrice.Value : null,
                        MaxPrice = searchRequest.MaxPrice.HasValue ? (double?)searchRequest.MaxPrice.Value : null,
                        SortBy = MapUnifiedToMaterialSort(searchRequest.SortBy),
                        SortDirection = searchRequest.SortDirection
                    },
                    materialPagination);

                var batchItems = batchResult.Items.ToList();

                if (batchItems.Count == 0)
                    break;

                foreach (var material in batchItems)
                {
                    // Unified search should return each base Material once,
                    // even when multiple nurseries import the same material.
                    if (!seenMaterialIds.Add(material.MaterialId))
                    {
                        continue;
                    }

                    if (collected.Count < windowSize)
                    {
                        collected.Add(new ShopSearchItemDto
                        {
                            Type = "Material",
                            Material = material
                        });
                    }
                }

                if (batchItems.Count < batchSize || pageNumber * batchSize >= batchResult.TotalCount)
                    break;

                pageNumber++;
            }

            return (collected, seenMaterialIds.Count);
        }

        private async Task<(List<ShopSearchItemDto> Items, int TotalCount)> FetchComboWindowItemsAsync(
            ShopUnifiedSearchRequestDto searchRequest,
            int windowSize)
        {
            if (!searchRequest.IncludeCombos || windowSize <= 0)
                return (new List<ShopSearchItemDto>(), 0);

            const int batchSize = 100;
            var collected = new List<ShopSearchItemDto>();
            var pageNumber = 1;
            var totalCount = 0;

            while (collected.Count < windowSize)
            {
                var comboPagination = new Pagination(pageNumber, batchSize);
                var batchResult = await _plantComboService.GetSellingCombosAsync(
                    comboPagination,
                    new PlantComboShopSearchRequestDto
                    {
                        Pagination = comboPagination,
                        NurseryId = searchRequest.NurseryId,
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

                totalCount = batchResult.TotalCount;
                var batchItems = batchResult.Items
                    .Select(c => new ShopSearchItemDto
                    {
                        Type = "Combo",
                        Combo = c
                    })
                    .ToList();

                if (batchItems.Count == 0)
                    break;

                collected.AddRange(batchItems);

                if (collected.Count >= totalCount)
                    break;

                pageNumber++;
            }

            return (collected.Take(windowSize).ToList(), totalCount);
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